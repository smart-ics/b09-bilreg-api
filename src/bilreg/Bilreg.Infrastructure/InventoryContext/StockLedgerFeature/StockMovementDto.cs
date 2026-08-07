using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockMovementDto(
    string StockMovementId,
    string SourceTransactionId,
    int MovementKind,
    DateTime EffectiveBusinessTime,
    int Origin,
    string ReversedMovementId,
    string CorrectedMovementId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockMovementDto FromModel(StockMovementModel model)
        => new(
            model.StockMovementId,
            model.SourceTransactionId,
            (int)model.MovementKind,
            model.EffectiveBusinessTime,
            (int)model.Origin,
            StockLedgerPersistenceSentinel.NullToEmpty(model.ReversedMovementId),
            StockLedgerPersistenceSentinel.NullToEmpty(model.CorrectedMovementId),
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockMovementModel ToModel(IEnumerable<StockMovementLineType> lines)
        => StockMovementModel.Rehydrate(
            StockMovementId,
            SourceTransactionReferenceType.Create(SourceTransactionId),
            (StockMovementKindEnum)MovementKind,
            EffectiveBusinessTime,
            (StockFactOriginEnum)Origin,
            lines,
            StockLedgerPersistenceSentinel.EmptyToNull(ReversedMovementId),
            StockLedgerPersistenceSentinel.EmptyToNull(CorrectedMovementId));
}
