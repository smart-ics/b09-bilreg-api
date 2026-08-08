using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// P4-S4 — Void a previously committed Native DO Receipt stock consequence.
/// Creates an accountable Reversal movement, depletes layers, applies legacy DO_V,
/// and refreshes fingerprint-v1 — behind the same capability flag as post.
/// </summary>
public sealed record VoidDoReceiptStockConsequenceCommand(
    string BrgId,
    string ReceiptSourceId,
    string OriginalSourceTransactionId,
    string VoidSourceTransactionId,
    DateTime EffectiveBusinessTime,
    DateTime ProcessedAt) : IRequest<VoidDoReceiptStockConsequenceResult>;

public enum VoidDoReceiptStockConsequenceOutcomeEnum
{
    Committed = 1,
    AlreadyCommitted = 2,
    Disabled = 3,
    NotEligible = 4,
    InsufficientStock = 5,
    Inconsistent = 6,
    StaleOrNotCurrent = 7
}

public sealed record VoidDoReceiptStockConsequenceResult(
    VoidDoReceiptStockConsequenceOutcomeEnum Outcome,
    StockLedgerScopeStateModel? ScopeState,
    string? StockMovementId,
    string? OriginalStockMovementId,
    SynchronizationPositionType? SynchronizationPosition,
    string? Explanation)
{
    public static VoidDoReceiptStockConsequenceResult Committed(
        StockLedgerScopeStateModel scope,
        string stockMovementId,
        string originalStockMovementId)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.Committed,
            scope,
            stockMovementId,
            originalStockMovementId,
            scope.SynchronizationPosition,
            Explanation: null);

    public static VoidDoReceiptStockConsequenceResult AlreadyCommitted(
        StockLedgerScopeStateModel? scope,
        string stockMovementId,
        string? originalStockMovementId)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.AlreadyCommitted,
            scope,
            stockMovementId,
            originalStockMovementId,
            scope?.SynchronizationPosition,
            Explanation: null);

    public static VoidDoReceiptStockConsequenceResult Disabled(StockLedgerScopeStateModel? scope)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.Disabled,
            scope,
            StockMovementId: null,
            OriginalStockMovementId: null,
            SynchronizationPosition: null,
            Explanation: "StockLedgerDoReceipt capability is disabled.");

    public static VoidDoReceiptStockConsequenceResult NotEligible(
        StockLedgerScopeStateModel? scope,
        string explanation)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.NotEligible,
            scope,
            StockMovementId: null,
            OriginalStockMovementId: null,
            SynchronizationPosition: scope?.SynchronizationPosition,
            Explanation: explanation);

    public static VoidDoReceiptStockConsequenceResult InsufficientStock(
        StockLedgerScopeStateModel scope,
        string explanation)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.InsufficientStock,
            scope,
            StockMovementId: null,
            OriginalStockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation);

    public static VoidDoReceiptStockConsequenceResult Inconsistent(
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.Inconsistent,
            scope,
            StockMovementId: null,
            OriginalStockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation ?? scope.InconsistencyReason);

    public static VoidDoReceiptStockConsequenceResult StaleOrNotCurrent(
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            VoidDoReceiptStockConsequenceOutcomeEnum.StaleOrNotCurrent,
            scope,
            StockMovementId: null,
            OriginalStockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation);
}

/// <summary>
/// P4-S4 — Thin MediatR handler for Native DO Receipt void consequence.
/// No production HTTP endpoint; tests compose manually with capability enabled.
/// </summary>
public sealed class VoidDoReceiptStockConsequenceHandler
    : IRequestHandler<VoidDoReceiptStockConsequenceCommand, VoidDoReceiptStockConsequenceResult>
{
    private readonly StockLedgerDoReceiptOptions _options;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockMovementRepo _movementRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockConsequenceUnitOfWork _consequenceUnitOfWork;
    private readonly LegacyStockFreshnessGate _freshnessGate;
    private readonly ILegacyStockReadPort _legacyStockReadPort;

    public VoidDoReceiptStockConsequenceHandler(
        IOptions<StockLedgerDoReceiptOptions> options,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockMovementRepo movementRepo,
        IStockPositionRepo positionRepo,
        IStockConsequenceUnitOfWork consequenceUnitOfWork,
        LegacyStockFreshnessGate freshnessGate,
        ILegacyStockReadPort legacyStockReadPort)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _scopeStateRepo = scopeStateRepo ?? throw new ArgumentNullException(nameof(scopeStateRepo));
        _idempotencyRepo = idempotencyRepo ?? throw new ArgumentNullException(nameof(idempotencyRepo));
        _movementRepo = movementRepo ?? throw new ArgumentNullException(nameof(movementRepo));
        _positionRepo = positionRepo ?? throw new ArgumentNullException(nameof(positionRepo));
        _consequenceUnitOfWork = consequenceUnitOfWork
            ?? throw new ArgumentNullException(nameof(consequenceUnitOfWork));
        _freshnessGate = freshnessGate ?? throw new ArgumentNullException(nameof(freshnessGate));
        _legacyStockReadPort = legacyStockReadPort
            ?? throw new ArgumentNullException(nameof(legacyStockReadPort));
    }

    public async Task<VoidDoReceiptStockConsequenceResult> Handle(
        VoidDoReceiptStockConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.ReceiptSourceId, nameof(request.ReceiptSourceId));
        Guard.Against.NullOrWhiteSpace(
            request.OriginalSourceTransactionId,
            nameof(request.OriginalSourceTransactionId));
        Guard.Against.NullOrWhiteSpace(
            request.VoidSourceTransactionId,
            nameof(request.VoidSourceTransactionId));
        Guard.Against.Default(request.EffectiveBusinessTime, nameof(request.EffectiveBusinessTime));
        Guard.Against.Default(request.ProcessedAt, nameof(request.ProcessedAt));

        var scopeKey = StockLedgerScopeKeyType.Create(request.BrgId, request.ReceiptSourceId);
        var loadedScope = _scopeStateRepo.LoadEntity(scopeKey);

        if (!_options.Enabled)
        {
            return VoidDoReceiptStockConsequenceResult.Disabled(
                loadedScope.HasValue ? loadedScope.Value : null);
        }

        var voidIdempotencyKey = DoReceiptVoidConsequenceIdempotency.BuildSourceConsequenceKey(
            scopeKey,
            request.VoidSourceTransactionId);
        var priorVoid = _idempotencyRepo.LoadByBusinessKey(
            StockSourceIdempotencyModel.BusinessKey(
                StockSourceIdempotencyKindEnum.SourceConsequence,
                voidIdempotencyKey));
        if (priorVoid.HasValue)
        {
            return VoidDoReceiptStockConsequenceResult.AlreadyCommitted(
                loadedScope.HasValue ? loadedScope.Value : null,
                priorVoid.Value.StockMovementId,
                originalStockMovementId: null);
        }

        if (!loadedScope.HasValue)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                null,
                "Scope is absent; Native DO Receipt void requires a Reconstructed baseline.");
        }

        var scope = loadedScope.Value;
        if (scope.ReconstructionStatus == ReconstructionStatusEnum.Inconsistent)
        {
            return VoidDoReceiptStockConsequenceResult.Inconsistent(
                scope,
                scope.InconsistencyReason
                ?? "Scope is Inconsistent; Native DO Receipt void is fail-closed.");
        }

        if (scope.ReconstructionStatus != ReconstructionStatusEnum.Reconstructed)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                $"Scope Reconstruction Status '{scope.ReconstructionStatus}' is not eligible "
                + "for Native DO Receipt void (expected Reconstructed).");
        }

        var gate = await _freshnessGate.EnsureFreshAsync(
            scopeKey,
            decisionContext: "VoidDoReceiptStockConsequence",
            cancellationToken);

        if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.Inconsistent)
        {
            return VoidDoReceiptStockConsequenceResult.Inconsistent(
                gate.ScopeState,
                gate.Explanation);
        }

        if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent
            || !gate.IsSafeToTrustLedgerLayers)
        {
            return VoidDoReceiptStockConsequenceResult.StaleOrNotCurrent(
                gate.ScopeState,
                gate.Explanation
                ?? "Freshness Gate did not prove scope current; Native DO Receipt void fail-closed.");
        }

        scope = gate.ScopeState;

        var originalIdempotencyKey = DoReceiptConsequenceIdempotency.BuildSourceConsequenceKey(
            scopeKey,
            request.OriginalSourceTransactionId);
        var originalIdempotency = _idempotencyRepo.LoadByBusinessKey(
            StockSourceIdempotencyModel.BusinessKey(
                StockSourceIdempotencyKindEnum.SourceConsequence,
                originalIdempotencyKey));
        if (!originalIdempotency.HasValue
            || string.IsNullOrWhiteSpace(originalIdempotency.Value.StockMovementId))
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                "Original Native DO Receipt SourceConsequence was not found; cannot void.");
        }

        var originalMovementKey = StockMovementModel.Key(originalIdempotency.Value.StockMovementId);
        var originalLoaded = _movementRepo.LoadEntity(originalMovementKey);
        if (!originalLoaded.HasValue)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                $"Original Stock Movement '{originalIdempotency.Value.StockMovementId}' was not found.");
        }

        var original = originalLoaded.Value;
        if (original.MovementKind != StockMovementKindEnum.Receipt
            || original.Origin != StockFactOriginEnum.Native)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                "Original movement is not a Native Receipt; P4-S4 void supports Native DO Receipt only.");
        }

        // Detect prior accountable reversal of this movement (history retained; do not double-void).
        var alreadyReversed = DetectPriorReversal(scopeKey, original.StockMovementId);
        if (alreadyReversed)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                $"Original movement '{original.StockMovementId}' already has an accountable Reversal.");
        }

        var positions = _positionRepo.ListByLedgerScope(scopeKey).ToList();
        var layerEligibility = TryValidateLayersFullyRemaining(original, positions, out var layerError);
        if (!layerEligibility)
        {
            return VoidDoReceiptStockConsequenceResult.InsufficientStock(scope, layerError!);
        }

        var balances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        var doJournals = journals
            .Where(j => string.Equals(j.MutationKindId, "DO", StringComparison.Ordinal))
            .ToList();

        if (doJournals.Count != original.Lines.Count)
        {
            return VoidDoReceiptStockConsequenceResult.NotEligible(
                scope,
                $"Legacy DO journal count ({doJournals.Count}) does not match original receipt "
                + $"line count ({original.Lines.Count}); void fails closed.");
        }

        if (!TryBuildVoidTargets(
                original,
                positions,
                balances,
                out var targets,
                out var targetError,
                out var insufficient))
        {
            return insufficient
                ? VoidDoReceiptStockConsequenceResult.InsufficientStock(scope, targetError!)
                : VoidDoReceiptStockConsequenceResult.NotEligible(scope, targetError!);
        }

        var voidSourceTx = SourceTransactionReferenceType.Key(request.VoidSourceTransactionId);
        var reversalMovementId = Ulid.NewUlid().ToString();
        var reversal = original.Reverse(
            voidSourceTx,
            request.EffectiveBusinessTime,
            StockFactOriginEnum.Native,
            reversalMovementId);

        var depletedPositions = DepletePositions(positions, reversal);

        var legacyWrite = DoReceiptVoidLegacyCompatibilityMapper.Map(
            voidSourceTx,
            scopeKey,
            request.EffectiveBusinessTime,
            targets);

        // Pre-commit projection keeps Scope inside the consequence UoW (P4-S2/S3 write order).
        // After commit, reconcile to live LegacyStockReadPort fingerprint-v1 so discovery matches
        // durable authority rows (same algorithm; corrects any DTO/read-back shape drift).
        var (projectedBalances, projectedJournals) =
            DoReceiptVoidLegacyCompatibilityMapper.ProjectPostVoidFingerprintSnapshot(
                balances,
                journals,
                legacyWrite);
        var nextPosition = LegacyReconstructionBasisCalculator.Compute(
            projectedBalances,
            projectedJournals);
        var refreshed = scope.RefreshSynchronizationPosition(nextPosition);

        var commit = _consequenceUnitOfWork.Commit(new StockConsequenceDraft(
            IdempotencyKey: voidIdempotencyKey,
            ProcessedAt: request.ProcessedAt,
            Movement: reversal,
            Positions: depletedPositions,
            ScopeState: refreshed,
            LegacyWrite: legacyWrite,
            IdempotencyKind: StockSourceIdempotencyKindEnum.SourceConsequence));

        if (commit.Outcome == StockConsequenceCommitOutcomeEnum.AlreadyCommitted)
        {
            var existingScope = _scopeStateRepo.LoadEntity(scopeKey);
            return VoidDoReceiptStockConsequenceResult.AlreadyCommitted(
                existingScope.HasValue ? existingScope.Value : refreshed,
                commit.StockMovementId,
                original.StockMovementId);
        }

        var reconciledScope = ReconcileSynchronizationPositionFromLiveAuthority(
            scopeKey,
            voidIdempotencyKey,
            request.VoidSourceTransactionId,
            request.ProcessedAt,
            commit.StockMovementId);

        return VoidDoReceiptStockConsequenceResult.Committed(
            reconciledScope,
            commit.StockMovementId,
            original.StockMovementId);
    }

    private StockLedgerScopeStateModel ReconcileSynchronizationPositionFromLiveAuthority(
        IStockLedgerScopeKey scopeKey,
        string voidIdempotencyKey,
        string voidSourceTransactionId,
        DateTime processedAt,
        string stockMovementId)
    {
        var persisted = _scopeStateRepo.LoadEntity(scopeKey);
        if (!persisted.HasValue)
        {
            throw new InvalidOperationException(
                "Scope missing after successful void commit; cannot reconcile Synchronization Position.");
        }

        var livePosition = LegacyReconstructionBasisCalculator.Compute(
            _legacyStockReadPort.ListCurrentBalances(scopeKey),
            _legacyStockReadPort.ListJournalEntries(scopeKey));

        if (Equals(persisted.Value.SynchronizationPosition, livePosition))
            return persisted.Value;

        var reconciled = persisted.Value.RefreshSynchronizationPosition(livePosition);
        _consequenceUnitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
            IdempotencyKey: $"{voidIdempotencyKey}|FP",
            ProcessedAt: processedAt,
            BrgId: scopeKey.BrgId,
            ReceiptSourceId: scopeKey.ReceiptSourceId,
            StockMovementId: stockMovementId,
            SourceTransactionId: voidSourceTransactionId,
            ScopeState: reconciled,
            ExpectedPriorSynchronizationState: SynchronizationStateEnum.Current));

        var reloaded = _scopeStateRepo.LoadEntity(scopeKey);
        return reloaded.HasValue ? reloaded.Value : reconciled;
    }

    private bool DetectPriorReversal(IStockLedgerScopeKey scopeKey, string originalMovementId)
    {
        // No reverse-index on ReversedMovementId. Durable signal for a completed Native/legacy void:
        // DO_V outbound qty already covers full DO inbound qty for this Receipt Source.
        _ = originalMovementId;
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        var doIn = journals
            .Where(j => string.Equals(j.MutationKindId, "DO", StringComparison.Ordinal))
            .Sum(j => j.QuantityIn);
        var doVOut = journals
            .Where(j => string.Equals(j.MutationKindId, "DO_V", StringComparison.Ordinal))
            .Sum(j => j.QuantityOut);

        return doIn > 0m && doVOut >= doIn;
    }

    private static bool TryValidateLayersFullyRemaining(
        StockMovementModel original,
        IReadOnlyList<StockPositionModel> positions,
        out string? error)
    {
        error = null;
        var layersById = positions
            .SelectMany(p => p.Layers)
            .ToDictionary(l => l.StockLayerId, StringComparer.Ordinal);

        foreach (var line in original.Lines.OrderBy(l => l.LineNo))
        {
            if (string.IsNullOrWhiteSpace(line.StockLayerId))
            {
                error =
                    $"Original receipt line {line.LineNo} has no StockLayerId; cannot void safely.";
                return false;
            }

            if (!layersById.TryGetValue(line.StockLayerId, out var layer))
            {
                error =
                    $"Original receipt layer '{line.StockLayerId}' was not found in Ledger positions.";
                return false;
            }

            if (layer.RemainingQuantity != layer.InitialQuantity)
            {
                error =
                    $"Layer '{layer.StockLayerId}' remaining {layer.RemainingQuantity} != initial "
                    + $"{layer.InitialQuantity}; stock was already consumed — void fails closed.";
                return false;
            }

            if (layer.RemainingQuantity != line.Quantity)
            {
                error =
                    $"Layer '{layer.StockLayerId}' quantity {layer.RemainingQuantity} does not match "
                    + $"receipt line quantity {line.Quantity}.";
                return false;
            }
        }

        return true;
    }

    private static bool TryBuildVoidTargets(
        StockMovementModel original,
        IReadOnlyList<StockPositionModel> positions,
        IReadOnlyList<LegacyStockBalanceType> balances,
        out List<DoReceiptVoidLegacyCompatibilityMapper.VoidLineTarget> targets,
        out string? error,
        out bool insufficient)
    {
        targets = [];
        error = null;
        insufficient = false;

        var layersById = positions
            .SelectMany(p => p.Layers)
            .ToDictionary(l => l.StockLayerId, StringComparer.Ordinal);
        var remainingBalances = balances.ToList();

        foreach (var line in original.Lines.OrderBy(l => l.LineNo))
        {
            layersById.TryGetValue(line.StockLayerId ?? string.Empty, out var layer);
            var expiration = layer?.ExpirationDate;
            var batch = layer?.Batch ?? string.Empty;

            var matchIndex = remainingBalances.FindIndex(b =>
                string.Equals(b.LayananId, line.LayananId, StringComparison.Ordinal)
                && b.Quantity == line.Quantity
                && Nullable.Equals(b.ExpirationDate, expiration)
                && string.Equals(b.Batch ?? string.Empty, batch, StringComparison.Ordinal));

            if (matchIndex < 0)
            {
                var candidates = remainingBalances
                    .Select((b, i) => (Balance: b, Index: i))
                    .Where(x =>
                        string.Equals(x.Balance.LayananId, line.LayananId, StringComparison.Ordinal)
                        && x.Balance.Quantity == line.Quantity)
                    .ToList();

                if (candidates.Count == 1)
                    matchIndex = candidates[0].Index;
                else if (candidates.Count == 0)
                {
                    insufficient = true;
                    error =
                        $"No legacy balance with qty {line.Quantity} at '{line.LayananId}' "
                        + "for void; insufficient stock or already consumed.";
                    return false;
                }
                else
                {
                    error =
                        $"Ambiguous legacy balance match for receipt line {line.LineNo} at "
                        + $"'{line.LayananId}'; void fails closed.";
                    return false;
                }
            }

            var balance = remainingBalances[matchIndex];
            if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
            {
                error = $"Legacy balance at '{line.LayananId}' has no LegacyRowId; cannot target void.";
                return false;
            }

            if (balance.Quantity < line.Quantity)
            {
                insufficient = true;
                error =
                    $"Legacy balance '{balance.LegacyRowId}' qty {balance.Quantity} < required "
                    + $"{line.Quantity}.";
                return false;
            }

            targets.Add(new DoReceiptVoidLegacyCompatibilityMapper.VoidLineTarget(
                LayananId: line.LayananId,
                Quantity: line.Quantity,
                UnitCost: balance.UnitCost,
                ExpirationDate: balance.ExpirationDate,
                Batch: balance.Batch,
                PurchaseOrderId: balance.PurchaseOrderId,
                LegacyRowId: balance.LegacyRowId!,
                SmallestUnitId: null));

            remainingBalances.RemoveAt(matchIndex);
        }

        return true;
    }

    private static IReadOnlyList<StockPositionModel> DepletePositions(
        IReadOnlyList<StockPositionModel> positions,
        StockMovementModel reversal)
    {
        var byLocation = positions.ToDictionary(p => p.LayananId, StringComparer.Ordinal);

        foreach (var line in reversal.Lines)
        {
            if (line.Direction != StockMovementDirectionEnum.Outbound)
                continue;
            if (string.IsNullOrWhiteSpace(line.StockLayerId))
            {
                throw new InvalidOperationException(
                    $"Reversal line {line.LineNo} requires StockLayerId for layer depletion.");
            }

            if (!byLocation.TryGetValue(line.LayananId, out var position))
            {
                throw new InvalidOperationException(
                    $"No Stock Position at '{line.LayananId}' for reversal depletion.");
            }

            byLocation[line.LayananId] = ConsumeLayer(position, line.StockLayerId, line.Quantity);
        }

        return byLocation.Values.ToList();
    }

    private static StockPositionModel ConsumeLayer(
        StockPositionModel position,
        string stockLayerId,
        decimal quantity)
    {
        var layers = new List<StockLayerModel>();
        var found = false;
        foreach (var layer in position.Layers)
        {
            if (!string.Equals(layer.StockLayerId, stockLayerId, StringComparison.Ordinal))
            {
                layers.Add(layer);
                continue;
            }

            found = true;
            layers.Add(layer.Consume(quantity));
        }

        if (!found)
        {
            throw new InvalidOperationException(
                $"Target layer '{stockLayerId}' was not found in position '{position.LayananId}'.");
        }

        return StockPositionModel.Create(position, layers, position.Version + 1);
    }
}
