using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S1 / G-08 trusted caller — Availability Discovery → reconstruct (if needed) →
/// Freshness Gate per candidate DO → reload Ledger layers → ED-constrained FIFO plan.
/// Returns a trusted allocation plan or a fail-closed outcome. Does not write FO stock,
/// persist positions, or invoke the Legacy Compatibility Writer.
/// </summary>
public sealed class TrustedStockAllocationOrchestrator
{
    private readonly IAvailabilityDiscoveryPort _availability;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly LegacyStockFreshnessGate _freshnessGate;
    private readonly Func<ReconstructStockLedgerBaselineCommand, CancellationToken, Task<ReconstructStockLedgerBaselineResult>> _reconstruct;

    public TrustedStockAllocationOrchestrator(
        IAvailabilityDiscoveryPort availability,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockPositionRepo positionRepo,
        LegacyStockFreshnessGate freshnessGate,
        ReconstructStockLedgerBaselineHandler reconstructHandler)
        : this(
            availability,
            scopeStateRepo,
            positionRepo,
            freshnessGate,
            reconstructHandler is null
                ? throw new ArgumentNullException(nameof(reconstructHandler))
                : reconstructHandler.Handle)
    {
    }

    /// <summary>
    /// Test-friendly constructor: inject a reconstruct delegate without a production interface.
    /// </summary>
    public TrustedStockAllocationOrchestrator(
        IAvailabilityDiscoveryPort availability,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockPositionRepo positionRepo,
        LegacyStockFreshnessGate freshnessGate,
        Func<ReconstructStockLedgerBaselineCommand, CancellationToken, Task<ReconstructStockLedgerBaselineResult>> reconstruct)
    {
        _availability = availability ?? throw new ArgumentNullException(nameof(availability));
        _scopeStateRepo = scopeStateRepo ?? throw new ArgumentNullException(nameof(scopeStateRepo));
        _positionRepo = positionRepo ?? throw new ArgumentNullException(nameof(positionRepo));
        _freshnessGate = freshnessGate ?? throw new ArgumentNullException(nameof(freshnessGate));
        _reconstruct = reconstruct ?? throw new ArgumentNullException(nameof(reconstruct));
    }

    public async Task<TrustedStockAllocationResult> PlanAsync(
        TrustedStockAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.Null(request.Item, nameof(request.Item));
        Guard.Against.NullOrWhiteSpace(request.Item.BrgId, nameof(request.Item.BrgId));
        Guard.Against.Null(request.SourceLocation, nameof(request.SourceLocation));
        Guard.Against.NullOrWhiteSpace(
            request.SourceLocation.LayananId,
            nameof(request.SourceLocation.LayananId));
        Guard.Against.NegativeOrZero(request.RequestedQuantity, nameof(request.RequestedQuantity));

        var decisionContext = string.IsNullOrWhiteSpace(request.DecisionContext)
            ? "TrustedStockAllocation"
            : request.DecisionContext;

        var discovery = _availability.Discover(
            request.Item,
            request.SourceLocation,
            request.ExpirationDateFilter);

        if (discovery.Outcome == AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock
            || discovery.Candidates.Count == 0)
        {
            return TrustedStockAllocationResult.InsufficientAuthoritativeStock(
                discovery.Candidates,
                discovery.Explanation
                ?? "Availability Discovery found insufficient authoritative stock at the source location.");
        }

        if (discovery.Outcome == AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent)
        {
            return TrustedStockAllocationResult.StaleOrNotCurrent(
                discovery.Candidates,
                discovery.Explanation
                ?? "Availability Discovery reported StaleOrNotCurrent; allocation fail-closed.");
        }

        var candidateDos = discovery.Candidates
            .Select(c => c.ReceiptSourceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        foreach (var receiptSourceId in candidateDos)
        {
            var prepare = await EnsureScopeTrustedAsync(
                request.Item.BrgId,
                receiptSourceId,
                discovery.Candidates,
                decisionContext,
                cancellationToken);
            if (prepare is not null)
                return prepare;
        }

        var layerPool = ReloadSourceLayers(
            request.Item.BrgId,
            request.SourceLocation.LayananId,
            candidateDos);

        var allocation = StockFifoAllocator.Allocate(
            layerPool,
            request.Item,
            request.SourceLocation,
            request.RequestedQuantity,
            request.ExpirationDateFilter);

        if (!allocation.IsFulfilled)
        {
            var available = layerPool.Sum(l => l.RemainingQuantity);
            return TrustedStockAllocationResult.InsufficientLedgerStock(
                discovery.Candidates,
                $"Ledger layers at '{request.SourceLocation.LayananId}' cannot fulfill "
                + $"{request.RequestedQuantity} after Freshness Gate and reload "
                + $"(available {available}).");
        }

        return TrustedStockAllocationResult.PlanReady(allocation, discovery.Candidates);
    }

    private async Task<TrustedStockAllocationResult?> EnsureScopeTrustedAsync(
        string brgId,
        string receiptSourceId,
        IReadOnlyList<AvailabilityCandidateType> candidates,
        string decisionContext,
        CancellationToken cancellationToken)
    {
        var scopeKey = StockLedgerScopeKeyType.Create(brgId, receiptSourceId);
        var loaded = _scopeStateRepo.LoadEntity(scopeKey);

        if (loaded.HasValue)
        {
            var scope = loaded.Value;
            if (scope.ReconstructionStatus == ReconstructionStatusEnum.Inconsistent)
            {
                return TrustedStockAllocationResult.Inconsistent(
                    candidates,
                    scope.InconsistencyReason
                    ?? $"Scope ({brgId}/{receiptSourceId}) is Inconsistent; allocation fail-closed.");
            }

            if (scope.ReconstructionStatus == ReconstructionStatusEnum.Reconstructing)
            {
                return TrustedStockAllocationResult.StaleOrNotCurrent(
                    candidates,
                    $"Scope ({brgId}/{receiptSourceId}) is Reconstructing; "
                    + "cannot trust layers until reconstruction completes.");
            }

            if (scope.ReconstructionStatus == ReconstructionStatusEnum.Reconstructed)
                return await GateOrFailAsync(scopeKey, candidates, decisionContext, cancellationToken);
        }

        // Absent, NotReconstructed, or ReconstructionRequired — reconstruct then gate.
        if (!loaded.HasValue
            || loaded.Value.ReconstructionStatus
                is ReconstructionStatusEnum.NotReconstructed
                or ReconstructionStatusEnum.ReconstructionRequired)
        {
            var recon = await _reconstruct(
                new ReconstructStockLedgerBaselineCommand(brgId, receiptSourceId),
                cancellationToken);

            switch (recon.Outcome)
            {
                case ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed:
                case ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed:
                    break;

                case ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent:
                case ReconstructStockLedgerBaselineOutcomeEnum.AlreadyInconsistent:
                    return TrustedStockAllocationResult.Inconsistent(
                        candidates,
                        recon.InconsistencyReason
                        ?? recon.ScopeState.InconsistencyReason
                        ?? $"Reconstruction left scope ({brgId}/{receiptSourceId}) Inconsistent.");

                case ReconstructStockLedgerBaselineOutcomeEnum.BasisChangedRetryRequired:
                    return TrustedStockAllocationResult.StaleOrNotCurrent(
                        candidates,
                        $"Reconstruction for ({brgId}/{receiptSourceId}) could not settle "
                        + "after bounded basis-change retries.");

                default:
                    return TrustedStockAllocationResult.StaleOrNotCurrent(
                        candidates,
                        $"Unexpected reconstruction outcome '{recon.Outcome}' "
                        + $"for ({brgId}/{receiptSourceId}).");
            }

            return await GateOrFailAsync(scopeKey, candidates, decisionContext, cancellationToken);
        }

        return TrustedStockAllocationResult.StaleOrNotCurrent(
            candidates,
            $"Scope ({brgId}/{receiptSourceId}) status '{loaded.Value.ReconstructionStatus}' "
            + "is not eligible for trusted allocation.");
    }

    private async Task<TrustedStockAllocationResult?> GateOrFailAsync(
        IStockLedgerScopeKey scopeKey,
        IReadOnlyList<AvailabilityCandidateType> candidates,
        string decisionContext,
        CancellationToken cancellationToken)
    {
        var gate = await _freshnessGate.EnsureFreshAsync(
            scopeKey,
            decisionContext,
            cancellationToken);

        if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.Inconsistent)
        {
            return TrustedStockAllocationResult.Inconsistent(
                candidates,
                gate.Explanation
                ?? gate.ScopeState.InconsistencyReason
                ?? $"Freshness Gate marked scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) Inconsistent.");
        }

        if (gate.Outcome == LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent
            || !gate.IsSafeToTrustLedgerLayers)
        {
            return TrustedStockAllocationResult.StaleOrNotCurrent(
                candidates,
                gate.Explanation
                ?? $"Freshness Gate did not prove scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) current.");
        }

        return null;
    }

    private List<StockLayerModel> ReloadSourceLayers(
        string brgId,
        string sourceLayananId,
        IReadOnlyList<string> receiptSourceIds)
    {
        var layers = new List<StockLayerModel>();
        foreach (var receiptSourceId in receiptSourceIds)
        {
            var writeScope = StockWriteScopeKeyType.Create(brgId, receiptSourceId, sourceLayananId);
            var position = _positionRepo.LoadEntity(writeScope);
            if (!position.HasValue)
                continue;

            foreach (var layer in position.Value.Layers)
            {
                if (layer.HasAvailableQuantity)
                    layers.Add(layer);
            }
        }

        return layers;
    }
}

public sealed record TrustedStockAllocationRequest(
    IBrgKey Item,
    ILayananKey SourceLocation,
    decimal RequestedQuantity,
    DateOnly? ExpirationDateFilter = null,
    string? DecisionContext = null);

public enum TrustedStockAllocationOutcomeEnum
{
    PlanReady = 1,
    InsufficientAuthoritativeStock = 2,
    InsufficientLedgerStock = 3,
    StaleOrNotCurrent = 4,
    Inconsistent = 5
}

public sealed record TrustedStockAllocationResult(
    TrustedStockAllocationOutcomeEnum Outcome,
    StockAllocationResult? AllocationPlan,
    IReadOnlyList<AvailabilityCandidateType>? ProvisionalCandidates,
    string? Explanation)
{
    public static TrustedStockAllocationResult PlanReady(
        StockAllocationResult plan,
        IReadOnlyList<AvailabilityCandidateType> candidates)
        => new(
            TrustedStockAllocationOutcomeEnum.PlanReady,
            plan,
            candidates,
            Explanation: null);

    public static TrustedStockAllocationResult InsufficientAuthoritativeStock(
        IReadOnlyList<AvailabilityCandidateType>? candidates,
        string? explanation)
        => new(
            TrustedStockAllocationOutcomeEnum.InsufficientAuthoritativeStock,
            AllocationPlan: null,
            candidates,
            explanation);

    public static TrustedStockAllocationResult InsufficientLedgerStock(
        IReadOnlyList<AvailabilityCandidateType>? candidates,
        string? explanation)
        => new(
            TrustedStockAllocationOutcomeEnum.InsufficientLedgerStock,
            AllocationPlan: null,
            candidates,
            explanation);

    public static TrustedStockAllocationResult StaleOrNotCurrent(
        IReadOnlyList<AvailabilityCandidateType>? candidates,
        string? explanation)
        => new(
            TrustedStockAllocationOutcomeEnum.StaleOrNotCurrent,
            AllocationPlan: null,
            candidates,
            explanation);

    public static TrustedStockAllocationResult Inconsistent(
        IReadOnlyList<AvailabilityCandidateType>? candidates,
        string? explanation)
        => new(
            TrustedStockAllocationOutcomeEnum.Inconsistent,
            AllocationPlan: null,
            candidates,
            explanation);
}
