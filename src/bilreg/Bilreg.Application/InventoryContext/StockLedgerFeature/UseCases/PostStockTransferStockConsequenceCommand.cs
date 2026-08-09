using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// P5-S3 — Authorized Stock Transfer line fact (already converted to stock unit).
/// Stock Ledger does not own Mutasi / Stock Transfer workflow UI.
/// </summary>
public sealed record StockTransferLineFact(
    int LineNumber,
    string BrgId,
    decimal Quantity,
    DateOnly? ExpirationDate = null);

/// <summary>
/// P5-S3 — Native Stock Transfer stock consequence behind capability flag.
/// Chains trusted allocation → CreateTransfer → MT legacy OUT/IN → Scope fingerprint refresh
/// through existing consequence UoW.
/// </summary>
public sealed record PostStockTransferStockConsequenceCommand(
    string SourceTransactionId,
    string SourceLocationId,
    string DestinationLocationId,
    DateTime EffectiveBusinessTime,
    IReadOnlyList<StockTransferLineFact> Lines,
    DateTime ProcessedAt) : IRequest<PostStockTransferStockConsequenceResult>;

public enum PostStockTransferStockConsequenceOutcomeEnum
{
    Committed = 1,
    AlreadyCommitted = 2,
    Disabled = 3,
    InsufficientStock = 4,
    StaleOrNotCurrent = 5,
    Inconsistent = 6
}

public sealed record PostStockTransferStockConsequenceResult(
    PostStockTransferStockConsequenceOutcomeEnum Outcome,
    StockLedgerScopeStateModel? PrimaryScopeState,
    string? StockMovementId,
    SynchronizationPositionType? SynchronizationPosition,
    string? Explanation)
{
    public static PostStockTransferStockConsequenceResult Committed(
        StockLedgerScopeStateModel primaryScope,
        string stockMovementId)
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.Committed,
            primaryScope,
            stockMovementId,
            primaryScope.SynchronizationPosition,
            Explanation: null);

    public static PostStockTransferStockConsequenceResult AlreadyCommitted(
        StockLedgerScopeStateModel? primaryScope,
        string stockMovementId)
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.AlreadyCommitted,
            primaryScope,
            stockMovementId,
            primaryScope?.SynchronizationPosition,
            Explanation: null);

    public static PostStockTransferStockConsequenceResult Disabled()
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.Disabled,
            PrimaryScopeState: null,
            StockMovementId: null,
            SynchronizationPosition: null,
            Explanation: "StockLedgerStockTransfer capability is disabled.");

    public static PostStockTransferStockConsequenceResult InsufficientStock(string explanation)
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.InsufficientStock,
            PrimaryScopeState: null,
            StockMovementId: null,
            SynchronizationPosition: null,
            Explanation: explanation);

    public static PostStockTransferStockConsequenceResult StaleOrNotCurrent(string explanation)
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.StaleOrNotCurrent,
            PrimaryScopeState: null,
            StockMovementId: null,
            SynchronizationPosition: null,
            Explanation: explanation);

    public static PostStockTransferStockConsequenceResult Inconsistent(string explanation)
        => new(
            PostStockTransferStockConsequenceOutcomeEnum.Inconsistent,
            PrimaryScopeState: null,
            StockMovementId: null,
            SynchronizationPosition: null,
            Explanation: explanation);
}

/// <summary>
/// P5-S3 — Thin MediatR handler for Native Stock Transfer consequence.
/// No production HTTP endpoint; tests compose manually with capability enabled.
/// </summary>
public sealed class PostStockTransferStockConsequenceHandler
    : IRequestHandler<PostStockTransferStockConsequenceCommand, PostStockTransferStockConsequenceResult>
{
    private readonly StockLedgerStockTransferOptions _options;
    private readonly TrustedStockAllocationOrchestrator _allocationOrchestrator;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockConsequenceUnitOfWork _consequenceUnitOfWork;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly LegacySyncIdentityBootstrapper _identityBootstrapper;
    private readonly StockLayerLegacyBindingResolver _bindingResolver;

    public PostStockTransferStockConsequenceHandler(
        IOptions<StockLedgerStockTransferOptions> options,
        TrustedStockAllocationOrchestrator allocationOrchestrator,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockPositionRepo positionRepo,
        IStockConsequenceUnitOfWork consequenceUnitOfWork,
        ILegacyStockReadPort legacyStockReadPort,
        LegacySyncIdentityBootstrapper identityBootstrapper,
        StockLayerLegacyBindingResolver bindingResolver)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _allocationOrchestrator = allocationOrchestrator
            ?? throw new ArgumentNullException(nameof(allocationOrchestrator));
        _scopeStateRepo = scopeStateRepo ?? throw new ArgumentNullException(nameof(scopeStateRepo));
        _idempotencyRepo = idempotencyRepo ?? throw new ArgumentNullException(nameof(idempotencyRepo));
        _positionRepo = positionRepo ?? throw new ArgumentNullException(nameof(positionRepo));
        _consequenceUnitOfWork = consequenceUnitOfWork
            ?? throw new ArgumentNullException(nameof(consequenceUnitOfWork));
        _legacyStockReadPort = legacyStockReadPort
            ?? throw new ArgumentNullException(nameof(legacyStockReadPort));
        _identityBootstrapper = identityBootstrapper
            ?? throw new ArgumentNullException(nameof(identityBootstrapper));
        _bindingResolver = bindingResolver
            ?? throw new ArgumentNullException(nameof(bindingResolver));
    }

    public async Task<PostStockTransferStockConsequenceResult> Handle(
        PostStockTransferStockConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SourceTransactionId, nameof(request.SourceTransactionId));
        Guard.Against.NullOrWhiteSpace(request.SourceLocationId, nameof(request.SourceLocationId));
        Guard.Against.NullOrWhiteSpace(request.DestinationLocationId, nameof(request.DestinationLocationId));
        Guard.Against.Default(request.EffectiveBusinessTime, nameof(request.EffectiveBusinessTime));
        Guard.Against.Default(request.ProcessedAt, nameof(request.ProcessedAt));
        Guard.Against.Null(request.Lines, nameof(request.Lines));
        if (request.Lines.Count == 0)
            throw new ArgumentException("At least one Stock Transfer line is required.", nameof(request));
        if (string.Equals(
                request.SourceLocationId.Trim(),
                request.DestinationLocationId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Source and destination Stock Locations must differ.",
                nameof(request));
        }

        if (!_options.Enabled)
            return PostStockTransferStockConsequenceResult.Disabled();

        var orderedLines = request.Lines.OrderBy(l => l.LineNumber).ToList();
        foreach (var line in orderedLines)
        {
            Guard.Against.NullOrWhiteSpace(line.BrgId, nameof(line.BrgId));
            Guard.Against.NegativeOrZero(line.Quantity, nameof(line.Quantity));
        }

        // Whole-mutasi identity: independent of line order / BrgId set / line count.
        var idempotencyKey = TransferConsequenceIdempotency.BuildSourceConsequenceKey(
            request.SourceTransactionId);
        var prior = _idempotencyRepo.LoadByBusinessKey(
            StockSourceIdempotencyModel.BusinessKey(
                StockSourceIdempotencyKindEnum.SourceConsequence,
                idempotencyKey));
        if (prior.HasValue)
        {
            var priorScope = LoadOptionalScope(prior.Value);
            return PostStockTransferStockConsequenceResult.AlreadyCommitted(
                priorScope,
                prior.Value.StockMovementId);
        }

        var sourceLocation = LayananType.Key(request.SourceLocationId);
        var destinationLocation = LayananType.Key(request.DestinationLocationId);
        var plans = new List<StockAllocationResult>(orderedLines.Count);

        foreach (var line in orderedLines)
        {
            var planResult = await _allocationOrchestrator.PlanAsync(
                new TrustedStockAllocationRequest(
                    BrgObatType.Key(line.BrgId),
                    sourceLocation,
                    line.Quantity,
                    line.ExpirationDate,
                    DecisionContext: "PostStockTransferStockConsequence"),
                cancellationToken);

            switch (planResult.Outcome)
            {
                case TrustedStockAllocationOutcomeEnum.PlanReady:
                    plans.Add(planResult.AllocationPlan!);
                    break;

                case TrustedStockAllocationOutcomeEnum.InsufficientAuthoritativeStock:
                case TrustedStockAllocationOutcomeEnum.InsufficientLedgerStock:
                    return PostStockTransferStockConsequenceResult.InsufficientStock(
                        planResult.Explanation
                        ?? $"Trusted allocation could not fulfill line {line.LineNumber}.");

                case TrustedStockAllocationOutcomeEnum.StaleOrNotCurrent:
                    return PostStockTransferStockConsequenceResult.StaleOrNotCurrent(
                        planResult.Explanation
                        ?? $"Trusted allocation reported StaleOrNotCurrent for line {line.LineNumber}.");

                case TrustedStockAllocationOutcomeEnum.Inconsistent:
                    return PostStockTransferStockConsequenceResult.Inconsistent(
                        planResult.Explanation
                        ?? $"Trusted allocation reported Inconsistent for line {line.LineNumber}.");

                default:
                    return PostStockTransferStockConsequenceResult.InsufficientStock(
                        $"Unexpected allocation outcome '{planResult.Outcome}' for line {line.LineNumber}.");
            }
        }

        var allAllocations = plans.SelectMany(p => p.Allocations).ToList();
        if (allAllocations.Count == 0)
        {
            return PostStockTransferStockConsequenceResult.InsufficientStock(
                "Trusted allocation produced no accountable layer slices.");
        }

        var movementId = Ulid.NewUlid().ToString();
        var movementKey = StockMovementModel.Key(movementId);
        var sourceTx = SourceTransactionReferenceType.Key(request.SourceTransactionId);

        var layersById = LoadSourceLayersById(allAllocations, request.SourceLocationId);
        var balancesForResolver = CollectSourceBalances(allAllocations);
        var buildResult = BuildTransferArtifacts(
            allAllocations,
            layersById,
            destinationLocation,
            movementKey,
            balancesForResolver,
            request.ProcessedAt);

        if (buildResult.Failure is { } failure)
            return failure;

        var movementLines = buildResult.MovementLines!;
        var destinationLayersByWriteScope = buildResult.DestinationLayersByWriteScope!;
        var transferSlices = buildResult.TransferSlices!;
        var layerBindings = buildResult.LayerBindings!;

        var movement = StockMovementModel.CreateTransfer(
            sourceTx,
            request.EffectiveBusinessTime,
            movementLines,
            StockFactOriginEnum.Native,
            stockMovementId: movementId);

        var sourcePositions = ApplySourceConsumption(allAllocations, request.SourceLocationId);
        var destinationPositions = BuildDestinationPositions(destinationLayersByWriteScope);
        var allPositions = sourcePositions.Concat(destinationPositions).ToList();

        var legacyWrite = TransferLegacyCompatibilityMapper.Map(
            sourceTx,
            request.SourceTransactionId,
            request.EffectiveBusinessTime,
            transferSlices);

        var affectedScopes = allAllocations
            .Select(a => StockLedgerScopeKeyType.Create(a.BrgId, a.ReceiptSourceId))
            .DistinctBy(s => $"{s.BrgId}|{s.ReceiptSourceId}", StringComparer.Ordinal)
            .ToList();

        var refreshedScopes = new List<StockLedgerScopeStateModel>(affectedScopes.Count);
        foreach (var scopeKey in affectedScopes)
        {
            var loaded = _scopeStateRepo.LoadEntity(scopeKey);
            if (!loaded.HasValue)
            {
                return PostStockTransferStockConsequenceResult.StaleOrNotCurrent(
                    $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) missing after trusted allocation.");
            }

            var scope = loaded.Value;
            if (scope.ReconstructionStatus != ReconstructionStatusEnum.Reconstructed
                || scope.SynchronizationState != SynchronizationStateEnum.Current
                || scope.SynchronizationPosition is null)
            {
                return PostStockTransferStockConsequenceResult.StaleOrNotCurrent(
                    $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) is not Reconstructed+Current "
                    + "after trusted allocation; transfer fail-closed.");
            }

            var priorBalances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
            var priorJournals = _legacyStockReadPort.ListJournalEntries(scopeKey);
            var (projectedBalances, projectedJournals) =
                TransferLegacyCompatibilityMapper.ProjectPostTransferFingerprintSnapshot(
                    scopeKey,
                    priorBalances,
                    priorJournals,
                    legacyWrite);
            var nextPosition = LegacyReconstructionBasisCalculator.Compute(
                projectedBalances,
                projectedJournals);
            refreshedScopes.Add(scope.RefreshSynchronizationPosition(nextPosition));
        }

        var primaryScope = refreshedScopes[0];
        var additionalScopes = refreshedScopes.Skip(1).ToList();

        var commit = _consequenceUnitOfWork.Commit(new StockConsequenceDraft(
            IdempotencyKey: idempotencyKey,
            ProcessedAt: request.ProcessedAt,
            Movement: movement,
            Positions: allPositions,
            ScopeState: primaryScope,
            LegacyWrite: legacyWrite,
            IdempotencyKind: StockSourceIdempotencyKindEnum.SourceConsequence,
            AdditionalScopeStates: additionalScopes.Count == 0 ? null : additionalScopes,
            LayerLegacyBindings: layerBindings.Count == 0 ? null : layerBindings));

        if (commit.Outcome == StockConsequenceCommitOutcomeEnum.AlreadyCommitted)
        {
            var existing = _scopeStateRepo.LoadEntity(
                StockLedgerScopeKeyType.Create(primaryScope.BrgId, primaryScope.ReceiptSourceId));
            return PostStockTransferStockConsequenceResult.AlreadyCommitted(
                existing.HasValue ? existing.Value : primaryScope,
                commit.StockMovementId);
        }

        foreach (var scopeKey in affectedScopes)
        {
            BootstrapDiscoveryIdentities(scopeKey, request.ProcessedAt);
            ReconcileSynchronizationPositionFromLiveAuthority(
                scopeKey,
                idempotencyKey,
                request.SourceTransactionId,
                request.ProcessedAt,
                commit.StockMovementId);
        }

        var persistedPrimary = _scopeStateRepo.LoadEntity(
            StockLedgerScopeKeyType.Create(primaryScope.BrgId, primaryScope.ReceiptSourceId));
        return PostStockTransferStockConsequenceResult.Committed(
            persistedPrimary.HasValue ? persistedPrimary.Value : primaryScope,
            commit.StockMovementId);
    }

    private Dictionary<string, StockLayerModel> LoadSourceLayersById(
        IReadOnlyList<StockLayerAllocationType> allocations,
        string sourceLocationId)
    {
        var result = new Dictionary<string, StockLayerModel>(StringComparer.Ordinal);
        var writeScopes = allocations
            .Select(a => StockWriteScopeKeyType.Create(a.BrgId, a.ReceiptSourceId, sourceLocationId))
            .DistinctBy(w => $"{w.BrgId}|{w.ReceiptSourceId}|{w.LayananId}", StringComparer.Ordinal);

        foreach (var writeScope in writeScopes)
        {
            var position = _positionRepo.LoadEntity(writeScope);
            if (!position.HasValue)
                continue;

            foreach (var layer in position.Value.Layers)
                result[layer.StockLayerId] = layer;
        }

        return result;
    }

    private sealed record TransferArtifactBuildResult(
        PostStockTransferStockConsequenceResult? Failure,
        List<StockMovementLineType>? MovementLines,
        Dictionary<string, List<StockLayerModel>>? DestinationLayersByWriteScope,
        List<TransferLegacyCompatibilityMapper.TransferAllocationSlice>? TransferSlices,
        List<StockLayerLegacyBindingType>? LayerBindings)
    {
        public static TransferArtifactBuildResult Ok(
            List<StockMovementLineType> movementLines,
            Dictionary<string, List<StockLayerModel>> destinationLayers,
            List<TransferLegacyCompatibilityMapper.TransferAllocationSlice> slices,
            List<StockLayerLegacyBindingType> bindings)
            => new(null, movementLines, destinationLayers, slices, bindings);

        public static TransferArtifactBuildResult Fail(PostStockTransferStockConsequenceResult failure)
            => new(failure, null, null, null, null);
    }

    private TransferArtifactBuildResult BuildTransferArtifacts(
        IReadOnlyList<StockLayerAllocationType> allocations,
        IReadOnlyDictionary<string, StockLayerModel> layersById,
        ILayananKey destinationLocation,
        IStockMovementKey movementKey,
        IReadOnlyList<LegacyStockBalanceType> sourceBalances,
        DateTime boundAt)
    {
        var movementLines = new List<StockMovementLineType>(allocations.Count * 2);
        var destinationLayers = new Dictionary<string, List<StockLayerModel>>(StringComparer.Ordinal);
        var slices = new List<TransferLegacyCompatibilityMapper.TransferAllocationSlice>(allocations.Count);
        var bindings = new List<StockLayerLegacyBindingType>(allocations.Count * 2);
        var claimedSourceRows = new HashSet<string>(StringComparer.Ordinal);
        var lineNo = 1;

        foreach (var allocation in allocations)
        {
            if (!layersById.TryGetValue(allocation.StockLayerId, out var sourceLayer))
            {
                throw new InvalidOperationException(
                    $"Allocated Stock Layer '{allocation.StockLayerId}' was not found in source positions.");
            }

            var resolution = _bindingResolver.ResolveSourceRow(
                sourceLayer,
                allocation.Quantity,
                sourceBalances,
                claimedSourceRows,
                boundAt);

            if (resolution.Kind == StockLayerLegacyBindingResolver.OutcomeKind.Ambiguous)
            {
                return TransferArtifactBuildResult.Fail(
                    PostStockTransferStockConsequenceResult.Inconsistent(resolution.Explanation));
            }

            if (resolution.Kind != StockLayerLegacyBindingResolver.OutcomeKind.Resolved
                || string.IsNullOrWhiteSpace(resolution.LegacyRowId))
            {
                return TransferArtifactBuildResult.Fail(
                    PostStockTransferStockConsequenceResult.InsufficientStock(resolution.Explanation));
            }

            if (resolution.BindingToPersist is not null)
                bindings.Add(resolution.BindingToPersist);

            var item = BrgObatType.Key(allocation.BrgId);
            var receiptSource = ReceiptSourceType.Key(allocation.ReceiptSourceId);
            var sourceLocation = LayananType.Key(allocation.LayananId);
            var destinationLegacyRowId = TransferLegacyCompatibilityMapper.NewCompactLegacyId("ST");

            movementLines.Add(StockMovementLineType.Create(
                lineNo++,
                item,
                receiptSource,
                sourceLocation,
                StockMovementDirectionEnum.Outbound,
                allocation.Quantity,
                allocation.UnitValuation,
                StockFactOriginEnum.Native,
                StockLayerModel.Key(allocation.StockLayerId)));

            var destLayer = StockLayerModel.Create(
                item,
                receiptSource,
                destinationLocation,
                movementKey,
                initialQuantity: allocation.Quantity,
                allocation.UnitValuation,
                sourceLayer.EffectiveReceiptTime,
                StockFactOriginEnum.Native,
                expirationDate: allocation.ExpirationDate ?? sourceLayer.ExpirationDate,
                batch: sourceLayer.Batch);

            movementLines.Add(StockMovementLineType.Create(
                lineNo++,
                item,
                receiptSource,
                destinationLocation,
                StockMovementDirectionEnum.Inbound,
                allocation.Quantity,
                allocation.UnitValuation,
                StockFactOriginEnum.Native,
                destLayer));

            var destWriteKey =
                $"{destLayer.BrgId}|{destLayer.ReceiptSourceId}|{destLayer.LayananId}";
            if (!destinationLayers.TryGetValue(destWriteKey, out var list))
            {
                list = [];
                destinationLayers[destWriteKey] = list;
            }

            list.Add(destLayer);
            bindings.Add(StockLayerLegacyBindingType.FromLayer(
                destLayer,
                destinationLegacyRowId,
                boundAt));

            slices.Add(new TransferLegacyCompatibilityMapper.TransferAllocationSlice(
                allocation.StockLayerId,
                resolution.LegacyRowId!,
                destinationLegacyRowId,
                allocation.BrgId,
                allocation.ReceiptSourceId,
                allocation.LayananId,
                destinationLocation.LayananId,
                allocation.Quantity,
                allocation.UnitValuation.AmountPerUnit,
                allocation.ExpirationDate ?? sourceLayer.ExpirationDate,
                sourceLayer.Batch,
                PurchaseOrderId: null,
                SmallestUnitId: null));
        }

        return TransferArtifactBuildResult.Ok(movementLines, destinationLayers, slices, bindings);
    }

    private List<StockPositionModel> ApplySourceConsumption(
        IReadOnlyList<StockLayerAllocationType> allocations,
        string sourceLocationId)
    {
        var byWriteScope = new Dictionary<string, StockPositionModel>(StringComparer.Ordinal);
        var consumesByScope = allocations
            .GroupBy(
                a => $"{a.BrgId}|{a.ReceiptSourceId}|{sourceLocationId}",
                StringComparer.Ordinal);

        foreach (var group in consumesByScope)
        {
            var sample = group.First();
            var writeScope = StockWriteScopeKeyType.Create(
                sample.BrgId,
                sample.ReceiptSourceId,
                sourceLocationId);
            var loaded = _positionRepo.LoadEntity(writeScope);
            if (!loaded.HasValue)
            {
                throw new InvalidOperationException(
                    $"Source Stock Position missing for '{group.Key}' after trusted allocation.");
            }

            var layers = loaded.Value.Layers.ToDictionary(l => l.StockLayerId, StringComparer.Ordinal);
            foreach (var allocation in group)
            {
                if (!layers.TryGetValue(allocation.StockLayerId, out var layer))
                {
                    throw new InvalidOperationException(
                        $"Target layer '{allocation.StockLayerId}' was not found in position '{group.Key}'.");
                }

                layers[allocation.StockLayerId] = layer.Consume(allocation.Quantity);
            }

            // One OCC version bump per write-scope regardless of how many layers were consumed.
            byWriteScope[group.Key] = StockPositionModel.Create(
                loaded.Value,
                layers.Values,
                loaded.Value.Version + 1);
        }

        return byWriteScope.Values.ToList();
    }

    private List<StockPositionModel> BuildDestinationPositions(
        Dictionary<string, List<StockLayerModel>> destinationLayersByWriteScope)
    {
        var result = new List<StockPositionModel>(destinationLayersByWriteScope.Count);
        foreach (var (_, inboundLayers) in destinationLayersByWriteScope)
        {
            var sample = inboundLayers[0];
            var writeScope = StockWriteScopeKeyType.Create(
                sample.BrgId,
                sample.ReceiptSourceId,
                sample.LayananId);
            var loaded = _positionRepo.LoadEntity(writeScope);
            var existingLayers = loaded.HasValue
                ? loaded.Value.Layers
                : Array.Empty<StockLayerModel>();
            var baseVersion = loaded.HasValue ? loaded.Value.Version : 0L;

            // One OCC version bump per write-scope regardless of how many inbound layers
            // are established/increased in this consequence (mirrors ApplySourceConsumption).
            result.Add(StockPositionModel.Create(
                writeScope,
                existingLayers.Concat(inboundLayers),
                baseVersion + 1));
        }

        return result;
    }

    private List<LegacyStockBalanceType> CollectSourceBalances(
        IReadOnlyList<StockLayerAllocationType> allocations)
    {
        var scopes = allocations
            .Select(a => StockLedgerScopeKeyType.Create(a.BrgId, a.ReceiptSourceId))
            .DistinctBy(s => $"{s.BrgId}|{s.ReceiptSourceId}", StringComparer.Ordinal);

        var balances = new List<LegacyStockBalanceType>();
        foreach (var scope in scopes)
            balances.AddRange(_legacyStockReadPort.ListCurrentBalances(scope));

        return balances;
    }

    private void BootstrapDiscoveryIdentities(IStockLedgerScopeKey scopeKey, DateTime processedAt)
    {
        var balances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        _identityBootstrapper.Bootstrap(scopeKey, journals, balances, processedAt);
    }

    private void ReconcileSynchronizationPositionFromLiveAuthority(
        IStockLedgerScopeKey scopeKey,
        string transferIdempotencyKey,
        string sourceTransactionId,
        DateTime processedAt,
        string stockMovementId)
    {
        var persisted = _scopeStateRepo.LoadEntity(scopeKey);
        if (!persisted.HasValue)
        {
            throw new InvalidOperationException(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) missing after transfer commit; "
                + "cannot reconcile Synchronization Position.");
        }

        var livePosition = LegacyReconstructionBasisCalculator.Compute(
            _legacyStockReadPort.ListCurrentBalances(scopeKey),
            _legacyStockReadPort.ListJournalEntries(scopeKey));

        if (Equals(persisted.Value.SynchronizationPosition, livePosition))
            return;

        var reconciled = persisted.Value.RefreshSynchronizationPosition(livePosition);
        _consequenceUnitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
            IdempotencyKey: $"{transferIdempotencyKey}|FP|{scopeKey.ReceiptSourceId}",
            ProcessedAt: processedAt,
            BrgId: scopeKey.BrgId,
            ReceiptSourceId: scopeKey.ReceiptSourceId,
            StockMovementId: stockMovementId,
            SourceTransactionId: sourceTransactionId,
            ScopeState: reconciled,
            ExpectedPriorSynchronizationState: SynchronizationStateEnum.Current));
    }

    private StockLedgerScopeStateModel? LoadOptionalScope(StockSourceIdempotencyModel prior)
    {
        if (string.IsNullOrWhiteSpace(prior.BrgId) || string.IsNullOrWhiteSpace(prior.ReceiptSourceId))
            return null;

        var loaded = _scopeStateRepo.LoadEntity(
            StockLedgerScopeKeyType.Create(prior.BrgId, prior.ReceiptSourceId));
        return loaded.HasValue ? loaded.Value : null;
    }
}
