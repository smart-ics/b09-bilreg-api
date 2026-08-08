using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// P4-S2 — Authorized DO Receipt line fact (already converted to stock unit).
/// Stock Ledger does not own Purchasing / Goods Receipt workflow.
/// </summary>
public sealed record DoReceiptLineFact(
    int LineNumber,
    string LayananId,
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpirationDate = null,
    string? Batch = null,
    string? PurchaseOrderId = null,
    string? SmallestUnitId = null);

/// <summary>
/// P4-S2 — Native DO Receipt stock consequence behind capability flag.
/// Posts Native Movement/Layer/Position, establishes Scope baseline + fingerprint-v1,
/// and applies live legacy DM post via existing consequence UoW.
/// </summary>
public sealed record PostDoReceiptStockConsequenceCommand(
    string BrgId,
    string ReceiptSourceId,
    string SourceTransactionId,
    DateTime EffectiveBusinessTime,
    IReadOnlyList<DoReceiptLineFact> Lines,
    DateTime ProcessedAt) : IRequest<PostDoReceiptStockConsequenceResult>;

public enum PostDoReceiptStockConsequenceOutcomeEnum
{
    Committed = 1,
    AlreadyCommitted = 2,
    Disabled = 3,
    ScopeNotEligible = 4,
    Inconsistent = 5,
    StaleOrNotCurrent = 6
}

public sealed record PostDoReceiptStockConsequenceResult(
    PostDoReceiptStockConsequenceOutcomeEnum Outcome,
    StockLedgerScopeStateModel? ScopeState,
    string? StockMovementId,
    SynchronizationPositionType? SynchronizationPosition,
    string? Explanation)
{
    public static PostDoReceiptStockConsequenceResult Committed(
        StockLedgerScopeStateModel scope,
        string stockMovementId)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.Committed,
            scope,
            stockMovementId,
            scope.SynchronizationPosition,
            Explanation: null);

    public static PostDoReceiptStockConsequenceResult AlreadyCommitted(
        StockLedgerScopeStateModel? scope,
        string stockMovementId)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.AlreadyCommitted,
            scope,
            stockMovementId,
            scope?.SynchronizationPosition,
            Explanation: null);

    public static PostDoReceiptStockConsequenceResult Disabled(StockLedgerScopeStateModel scope)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.Disabled,
            scope,
            StockMovementId: null,
            SynchronizationPosition: null,
            Explanation: "StockLedgerDoReceipt capability is disabled.");

    public static PostDoReceiptStockConsequenceResult ScopeNotEligible(
        StockLedgerScopeStateModel scope,
        string explanation)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.ScopeNotEligible,
            scope,
            StockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation);

    public static PostDoReceiptStockConsequenceResult Inconsistent(
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.Inconsistent,
            scope,
            StockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation ?? scope.InconsistencyReason);

    public static PostDoReceiptStockConsequenceResult StaleOrNotCurrent(
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            PostDoReceiptStockConsequenceOutcomeEnum.StaleOrNotCurrent,
            scope,
            StockMovementId: null,
            SynchronizationPosition: scope.SynchronizationPosition,
            Explanation: explanation);
}

/// <summary>
/// P4-S2 — Thin MediatR handler for Native DO Receipt consequence.
/// No production HTTP endpoint; tests compose manually with capability enabled.
/// </summary>
public sealed class PostDoReceiptStockConsequenceHandler
    : IRequestHandler<PostDoReceiptStockConsequenceCommand, PostDoReceiptStockConsequenceResult>
{
    private readonly StockLedgerDoReceiptOptions _options;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockConsequenceUnitOfWork _consequenceUnitOfWork;
    private readonly LegacyStockFreshnessGate _freshnessGate;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly LegacySyncIdentityBootstrapper _identityBootstrapper;

    public PostDoReceiptStockConsequenceHandler(
        IOptions<StockLedgerDoReceiptOptions> options,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockPositionRepo positionRepo,
        IStockConsequenceUnitOfWork consequenceUnitOfWork,
        LegacyStockFreshnessGate freshnessGate,
        ILegacyStockReadPort legacyStockReadPort,
        LegacySyncIdentityBootstrapper identityBootstrapper)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _scopeStateRepo = scopeStateRepo ?? throw new ArgumentNullException(nameof(scopeStateRepo));
        _idempotencyRepo = idempotencyRepo ?? throw new ArgumentNullException(nameof(idempotencyRepo));
        _positionRepo = positionRepo ?? throw new ArgumentNullException(nameof(positionRepo));
        _consequenceUnitOfWork = consequenceUnitOfWork
            ?? throw new ArgumentNullException(nameof(consequenceUnitOfWork));
        _freshnessGate = freshnessGate ?? throw new ArgumentNullException(nameof(freshnessGate));
        _legacyStockReadPort = legacyStockReadPort
            ?? throw new ArgumentNullException(nameof(legacyStockReadPort));
        _identityBootstrapper = identityBootstrapper
            ?? throw new ArgumentNullException(nameof(identityBootstrapper));
    }

    public async Task<PostDoReceiptStockConsequenceResult> Handle(
        PostDoReceiptStockConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.ReceiptSourceId, nameof(request.ReceiptSourceId));
        Guard.Against.NullOrWhiteSpace(request.SourceTransactionId, nameof(request.SourceTransactionId));
        Guard.Against.Default(request.EffectiveBusinessTime, nameof(request.EffectiveBusinessTime));
        Guard.Against.Default(request.ProcessedAt, nameof(request.ProcessedAt));
        Guard.Against.Null(request.Lines, nameof(request.Lines));
        if (request.Lines.Count == 0)
            throw new ArgumentException("At least one DO receipt line is required.", nameof(request));

        var scopeKey = StockLedgerScopeKeyType.Create(request.BrgId, request.ReceiptSourceId);
        var absentOrLoaded = LoadOrCreateNotReconstructed(scopeKey);

        if (!_options.Enabled)
            return PostDoReceiptStockConsequenceResult.Disabled(absentOrLoaded);

        var idempotencyKey = DoReceiptConsequenceIdempotency.BuildSourceConsequenceKey(
            scopeKey,
            request.SourceTransactionId);
        var prior = _idempotencyRepo.LoadByBusinessKey(
            StockSourceIdempotencyModel.BusinessKey(
                StockSourceIdempotencyKindEnum.SourceConsequence,
                idempotencyKey));
        if (prior.HasValue)
        {
            var existingScope = _scopeStateRepo.LoadEntity(scopeKey);
            return PostDoReceiptStockConsequenceResult.AlreadyCommitted(
                existingScope.HasValue ? existingScope.Value : absentOrLoaded,
                prior.Value.StockMovementId);
        }

        if (absentOrLoaded.ReconstructionStatus == ReconstructionStatusEnum.Inconsistent)
        {
            return PostDoReceiptStockConsequenceResult.Inconsistent(
                absentOrLoaded,
                absentOrLoaded.InconsistencyReason
                ?? "Scope is Inconsistent; Native DO Receipt is fail-closed.");
        }

        if (absentOrLoaded.ReconstructionStatus == ReconstructionStatusEnum.Reconstructed)
        {
            var gate = await _freshnessGate.EnsureFreshAsync(
                scopeKey,
                decisionContext: "PostDoReceiptStockConsequence",
                cancellationToken);

            if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.Inconsistent)
            {
                return PostDoReceiptStockConsequenceResult.Inconsistent(
                    gate.ScopeState,
                    gate.Explanation);
            }

            if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent
                || !gate.IsSafeToTrustLedgerLayers)
            {
                return PostDoReceiptStockConsequenceResult.StaleOrNotCurrent(
                    gate.ScopeState,
                    gate.Explanation
                    ?? "Freshness Gate did not prove scope current; Native DO Receipt fail-closed.");
            }

            // P4-S2: greenfield establish only — do not post onto an already-baselined scope.
            return PostDoReceiptStockConsequenceResult.ScopeNotEligible(
                gate.ScopeState,
                "Scope is already Reconstructed; P4-S2 Native DO Receipt posts only from NotReconstructed.");
        }

        if (absentOrLoaded.ReconstructionStatus != ReconstructionStatusEnum.NotReconstructed)
        {
            return PostDoReceiptStockConsequenceResult.ScopeNotEligible(
                absentOrLoaded,
                $"Scope Reconstruction Status '{absentOrLoaded.ReconstructionStatus}' is not eligible "
                + "for Native DO Receipt establish (expected NotReconstructed).");
        }

        // Greenfield only when Scope is NotReconstructed AND Item+DO has no prior
        // legacy authority or Ledger positions (Phase 4 §2.4). Fail closed otherwise —
        // do not invent a second post path for reconstructed/existing scopes.
        if (HasPriorStockHistory(scopeKey))
        {
            return PostDoReceiptStockConsequenceResult.ScopeNotEligible(
                absentOrLoaded,
                "Scope has prior legacy and/or Ledger stock history for Item+Receipt Source; "
                + "P4-S2 Native DO Receipt posts only on a genuinely empty greenfield scope.");
        }

        var sourceTx = SourceTransactionReferenceType.Key(request.SourceTransactionId);
        var movementId = Ulid.NewUlid().ToString();
        var movementKey = StockMovementModel.Key(movementId);
        var item = new BrgReff(request.BrgId.Trim(), request.BrgId.Trim());
        var receiptSource = ReceiptSourceType.Key(request.ReceiptSourceId);

        var lines = new List<StockMovementLineType>(request.Lines.Count);
        var layersByLocation = new Dictionary<string, List<StockLayerModel>>(StringComparer.Ordinal);

        foreach (var lineFact in request.Lines.OrderBy(l => l.LineNumber))
        {
            Guard.Against.NullOrWhiteSpace(lineFact.LayananId, nameof(lineFact.LayananId));
            Guard.Against.NegativeOrZero(lineFact.Quantity, nameof(lineFact.Quantity));

            var location = LayananType.Key(lineFact.LayananId);
            var layer = StockLayerModel.Create(
                item,
                receiptSource,
                location,
                movementKey,
                initialQuantity: lineFact.Quantity,
                UnitValuationType.Create(lineFact.UnitCost),
                request.EffectiveBusinessTime,
                StockFactOriginEnum.Native,
                expirationDate: lineFact.ExpirationDate,
                batch: lineFact.Batch);

            lines.Add(StockMovementLineType.Create(
                lineFact.LineNumber,
                item,
                receiptSource,
                location,
                StockMovementDirectionEnum.Inbound,
                lineFact.Quantity,
                UnitValuationType.Create(lineFact.UnitCost),
                StockFactOriginEnum.Native,
                layer));

            if (!layersByLocation.TryGetValue(location.LayananId, out var list))
            {
                list = [];
                layersByLocation[location.LayananId] = list;
            }

            list.Add(layer);
        }

        var movement = StockMovementModel.CreateReceipt(
            sourceTx,
            request.EffectiveBusinessTime,
            lines,
            StockFactOriginEnum.Native,
            stockMovementId: movementId);

        var positions = layersByLocation
            .Select(kvp =>
            {
                var position = StockPositionModel.CreateEmpty(
                    item,
                    receiptSource,
                    LayananType.Key(kvp.Key));
                foreach (var layer in kvp.Value)
                    position = position.AddLayer(layer);
                return position;
            })
            .ToList();

        var legacyWrite = DoReceiptLegacyCompatibilityMapper.Map(
            sourceTx,
            scopeKey,
            request.EffectiveBusinessTime,
            request.Lines);

        var snapshot = DoReceiptLegacyCompatibilityMapper.ToFingerprintSnapshot(legacyWrite);
        var position = LegacyReconstructionBasisCalculator.Compute(
            snapshot.Balances,
            snapshot.Journals);

        var established = absentOrLoaded.EstablishFromNativeReceipt(
            position,
            LegacyReconstructionBasisCalculator.AlgorithmVersion);

        var commit = _consequenceUnitOfWork.Commit(new StockConsequenceDraft(
            IdempotencyKey: idempotencyKey,
            ProcessedAt: request.ProcessedAt,
            Movement: movement,
            Positions: positions,
            ScopeState: established,
            LegacyWrite: legacyWrite,
            IdempotencyKind: StockSourceIdempotencyKindEnum.SourceConsequence));

        if (commit.Outcome == StockConsequenceCommitOutcomeEnum.AlreadyCommitted)
        {
            var existingScope = _scopeStateRepo.LoadEntity(scopeKey);
            return PostDoReceiptStockConsequenceResult.AlreadyCommitted(
                existingScope.HasValue ? existingScope.Value : established,
                commit.StockMovementId);
        }

        BootstrapDiscoveryIdentities(scopeKey, request.ProcessedAt);

        var persistedScope = _scopeStateRepo.LoadEntity(scopeKey);
        return PostDoReceiptStockConsequenceResult.Committed(
            persistedScope.HasValue ? persistedScope.Value : established,
            commit.StockMovementId);
    }

    private StockLedgerScopeStateModel LoadOrCreateNotReconstructed(IStockLedgerScopeKey scopeKey)
    {
        var loaded = _scopeStateRepo.LoadEntity(scopeKey);
        return loaded.HasValue
            ? loaded.Value
            : StockLedgerScopeStateModel.CreateNotReconstructed(scopeKey);
    }

    private bool HasPriorStockHistory(IStockLedgerScopeKey scopeKey)
    {
        if (_legacyStockReadPort.ListCurrentBalances(scopeKey).Count > 0)
            return true;
        if (_legacyStockReadPort.ListJournalEntries(scopeKey).Count > 0)
            return true;
        return _positionRepo.ListByLedgerScope(scopeKey).Count > 0;
    }

    private void BootstrapDiscoveryIdentities(IStockLedgerScopeKey scopeKey, DateTime processedAt)
    {
        var balances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        _identityBootstrapper.Bootstrap(scopeKey, journals, balances, processedAt);
    }
}
