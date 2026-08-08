using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockLedgerScopeDto(
    string BrgId,
    string ReceiptSourceId,
    int ReconstructionStatus,
    int SynchronizationState,
    byte[] SynchronizationPositionOpaque,
    string AlgorithmVersion,
    string ReconstructionBasisVersion,
    string InconsistencyReason,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockLedgerScopeDto FromModel(StockLedgerScopeStateModel model)
        => new(
            model.BrgId,
            model.ReceiptSourceId,
            (int)model.ReconstructionStatus,
            (int)model.SynchronizationState,
            StockLedgerPersistenceSentinel.OpaqueToStorage(model.SynchronizationPosition),
            StockLedgerPersistenceSentinel.AlgorithmVersionToStorage(model.SynchronizationPosition),
            StockLedgerPersistenceSentinel.NullToEmpty(model.ReconstructionBasisVersion),
            StockLedgerPersistenceSentinel.NullToEmpty(model.InconsistencyReason),
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockLedgerScopeStateModel ToModel()
        => StockLedgerScopeStateModel.Rehydrate(
            BrgId,
            ReceiptSourceId,
            (ReconstructionStatusEnum)ReconstructionStatus,
            (SynchronizationStateEnum)SynchronizationState,
            StockLedgerPersistenceSentinel.PositionFromStorage(
                SynchronizationPositionOpaque,
                AlgorithmVersion),
            StockLedgerPersistenceSentinel.EmptyToNull(ReconstructionBasisVersion),
            StockLedgerPersistenceSentinel.EmptyToNull(InconsistencyReason));
}
