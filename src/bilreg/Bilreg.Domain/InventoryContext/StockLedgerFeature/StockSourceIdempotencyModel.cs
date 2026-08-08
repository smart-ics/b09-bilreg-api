using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Durable uniqueness record for one source consequence or sync batch key (G-07).
/// Second insert of the same (Kind, Key) must not invent a second Ledger consequence.
/// </summary>
public record StockSourceIdempotencyModel :
    IStockSourceIdempotencyKey,
    IStockSourceIdempotencyBusinessKey
{
    #region CREATION
    private StockSourceIdempotencyModel(
        string idempotencyId,
        StockSourceIdempotencyKindEnum idempotencyKind,
        string idempotencyKey,
        string sourceTransactionId,
        string stockMovementId,
        string brgId,
        string receiptSourceId,
        DateTime processedAt)
    {
        Guard.Against.NullOrWhiteSpace(idempotencyId, nameof(idempotencyId));
        Guard.Against.EnumOutOfRange(idempotencyKind, nameof(idempotencyKind));
        Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));
        Guard.Against.Default(processedAt, nameof(processedAt));

        IdempotencyId = idempotencyId;
        IdempotencyKind = idempotencyKind;
        IdempotencyKey = idempotencyKey;
        SourceTransactionId = sourceTransactionId ?? string.Empty;
        StockMovementId = stockMovementId ?? string.Empty;
        BrgId = brgId ?? string.Empty;
        ReceiptSourceId = receiptSourceId ?? string.Empty;
        ProcessedAt = processedAt;
    }

    public static StockSourceIdempotencyModel Create(
        StockSourceIdempotencyKindEnum idempotencyKind,
        string idempotencyKey,
        DateTime processedAt,
        string? sourceTransactionId = null,
        string? stockMovementId = null,
        string? brgId = null,
        string? receiptSourceId = null,
        string? idempotencyId = null)
        => new(
            string.IsNullOrWhiteSpace(idempotencyId)
                ? Ulid.NewUlid().ToString()
                : idempotencyId,
            idempotencyKind,
            idempotencyKey.Trim(),
            string.IsNullOrWhiteSpace(sourceTransactionId) ? string.Empty : sourceTransactionId.Trim(),
            string.IsNullOrWhiteSpace(stockMovementId) ? string.Empty : stockMovementId.Trim(),
            string.IsNullOrWhiteSpace(brgId) ? string.Empty : brgId.Trim(),
            string.IsNullOrWhiteSpace(receiptSourceId) ? string.Empty : receiptSourceId.Trim(),
            processedAt);

    public static StockSourceIdempotencyModel Rehydrate(
        string idempotencyId,
        StockSourceIdempotencyKindEnum idempotencyKind,
        string idempotencyKey,
        string sourceTransactionId,
        string stockMovementId,
        string brgId,
        string receiptSourceId,
        DateTime processedAt)
        => new(
            idempotencyId,
            idempotencyKind,
            idempotencyKey,
            sourceTransactionId,
            stockMovementId,
            brgId,
            receiptSourceId,
            processedAt);

    public static IStockSourceIdempotencyKey Key(string idempotencyId)
        => new StockSourceIdempotencySurrogateKey(idempotencyId);

    public static IStockSourceIdempotencyBusinessKey BusinessKey(
        StockSourceIdempotencyKindEnum idempotencyKind,
        string idempotencyKey)
        => new StockSourceIdempotencyBusinessKey(idempotencyKind, idempotencyKey);
    #endregion

    #region PROPERTIES
    public string IdempotencyId { get; init; }
    public StockSourceIdempotencyKindEnum IdempotencyKind { get; init; }
    public string IdempotencyKey { get; init; }
    public string SourceTransactionId { get; init; }
    public string StockMovementId { get; init; }
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public DateTime ProcessedAt { get; init; }
    #endregion

    private sealed record StockSourceIdempotencySurrogateKey(string IdempotencyId)
        : IStockSourceIdempotencyKey;

    private sealed record StockSourceIdempotencyBusinessKey(
        StockSourceIdempotencyKindEnum IdempotencyKind,
        string IdempotencyKey) : IStockSourceIdempotencyBusinessKey;
}

public interface IStockSourceIdempotencyKey
{
    string IdempotencyId { get; }
}

public interface IStockSourceIdempotencyBusinessKey
{
    StockSourceIdempotencyKindEnum IdempotencyKind { get; }
    string IdempotencyKey { get; }
}
