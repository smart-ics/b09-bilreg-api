using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S3 / G-16 P0 — Live material reconciliation adapter (classify only).
/// Read-only compare of authoritative <c>tb_stok</c> remaining quantity vs Ledger Remaining Quantity
/// across all Stock Locations. Does not repair, mutate Movements/Layers/Positions/Scope,
/// or advance Synchronization Position.
/// </summary>
public sealed class StockReconciliationPort : IStockReconciliationPort
{
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly IStockLayerDal _layerDal;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;

    public StockReconciliationPort(
        ILegacyStockReadPort legacyStockReadPort,
        IStockLayerDal layerDal,
        IStockLedgerScopeStateRepo scopeStateRepo)
    {
        _legacyStockReadPort = legacyStockReadPort;
        _layerDal = layerDal;
        _scopeStateRepo = scopeStateRepo;
    }

    public StockReconciliationResult Reconcile(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var scopeState = _scopeStateRepo.LoadEntity(scope);
        if (!scopeState.HasValue
            || scopeState.Value.ReconstructionStatus != ReconstructionStatusEnum.Reconstructed)
        {
            return new StockReconciliationResult(
                StockReconciliationOutcomeEnum.ProvenanceLimitation,
                LegacyRemainingQuantity: null,
                LedgerRemainingQuantity: null,
                DifferenceQuantity: null,
                Differences: Array.Empty<StockReconciliationDifferenceType>(),
                Explanation:
                    "Scope has no reconstructed Stock Ledger baseline; material reconciliation "
                    + "requires Reconstruction Status Reconstructed.");
        }

        var balances = _legacyStockReadPort.ListCurrentBalances(scope);
        var layerDtos = _layerDal.ListByLedgerScope(scope);
        var layerSnapshots = layerDtos
            .Select(dto =>
            {
                var model = dto.ToModel();
                return new StockReconciliationClassifier.LedgerLayerSnapshot(
                    model.LayananId,
                    model.RemainingQuantity,
                    model.IsDepleted);
            })
            .ToList();

        var classified = StockReconciliationClassifier.Classify(scope, balances, layerSnapshots);

        // Material inconsistency always wins over pending-sync overlay.
        if (classified.Outcome == StockReconciliationOutcomeEnum.MaterialInconsistency)
            return classified;

        var syncState = scopeState.Value.SynchronizationState;
        if (syncState is SynchronizationStateEnum.SynchronizationRequired
            or SynchronizationStateEnum.LegacyChangePending)
        {
            return new StockReconciliationResult(
                StockReconciliationOutcomeEnum.PendingSynchronization,
                classified.LegacyRemainingQuantity,
                classified.LedgerRemainingQuantity,
                classified.DifferenceQuantity,
                classified.Differences,
                $"Scope Synchronization State is '{syncState}'; material quantities are consistent "
                + "(material-safe for catch-up completion) while Scope remains in a "
                + "synchronization-required lifecycle state.");
        }

        return classified;
    }
}
