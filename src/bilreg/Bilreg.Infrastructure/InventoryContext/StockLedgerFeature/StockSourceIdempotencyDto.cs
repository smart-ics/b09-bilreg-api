using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockSourceIdempotencyDto(
    string IdempotencyId,
    int IdempotencyKind,
    string IdempotencyKey,
    string SourceTransactionId,
    string StockMovementId,
    string BrgId,
    string ReceiptSourceId,
    DateTime ProcessedAt,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockSourceIdempotencyDto FromModel(StockSourceIdempotencyModel model)
        => new(
            model.IdempotencyId,
            (int)model.IdempotencyKind,
            model.IdempotencyKey,
            StockLedgerPersistenceSentinel.NullToEmpty(model.SourceTransactionId),
            StockLedgerPersistenceSentinel.NullToEmpty(model.StockMovementId),
            StockLedgerPersistenceSentinel.NullToEmpty(model.BrgId),
            StockLedgerPersistenceSentinel.NullToEmpty(model.ReceiptSourceId),
            model.ProcessedAt,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockSourceIdempotencyModel ToModel()
        => StockSourceIdempotencyModel.Rehydrate(
            IdempotencyId,
            (StockSourceIdempotencyKindEnum)IdempotencyKind,
            IdempotencyKey,
            SourceTransactionId,
            StockMovementId,
            BrgId,
            ReceiptSourceId,
            ProcessedAt);
}
