using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — Application coexistence projection linking a Ledger Stock Layer to the
/// Stage B authoritative <c>tb_stok</c> row (<c>LegacyRowId</c> / <c>fs_kd_trs</c>).
/// Not a Domain identity and not an authority claim.
/// </summary>
public sealed record StockLayerLegacyBindingType
{
    public StockLayerLegacyBindingType(
        string stockLayerId,
        string legacyRowId,
        string brgId,
        string receiptSourceId,
        string layananId,
        decimal amountPerUnit,
        DateOnly? expirationDate,
        string? batch,
        DateTime boundAt)
    {
        Guard.Against.NullOrWhiteSpace(stockLayerId, nameof(stockLayerId));
        Guard.Against.NullOrWhiteSpace(legacyRowId, nameof(legacyRowId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.Default(boundAt, nameof(boundAt));

        StockLayerId = stockLayerId.Trim();
        LegacyRowId = legacyRowId.Trim();
        BrgId = brgId.Trim();
        ReceiptSourceId = receiptSourceId.Trim();
        LayananId = layananId.Trim();
        AmountPerUnit = amountPerUnit;
        ExpirationDate = expirationDate;
        Batch = string.IsNullOrWhiteSpace(batch) ? null : batch.Trim();
        BoundAt = boundAt;
    }

    public string StockLayerId { get; }
    public string LegacyRowId { get; }
    public string BrgId { get; }
    public string ReceiptSourceId { get; }
    public string LayananId { get; }
    public decimal AmountPerUnit { get; }
    public DateOnly? ExpirationDate { get; }
    public string? Batch { get; }
    public DateTime BoundAt { get; }

    public static StockLayerLegacyBindingType Create(
        string stockLayerId,
        string legacyRowId,
        IStockWriteScopeKey writeScope,
        decimal amountPerUnit,
        DateOnly? expirationDate,
        string? batch,
        DateTime boundAt)
        => new(
            stockLayerId,
            legacyRowId,
            writeScope.BrgId,
            writeScope.ReceiptSourceId,
            writeScope.LayananId,
            amountPerUnit,
            expirationDate,
            batch,
            boundAt);

    public static StockLayerLegacyBindingType FromLayer(
        StockLayerModel layer,
        string legacyRowId,
        DateTime boundAt)
        => new(
            layer.StockLayerId,
            legacyRowId,
            layer.BrgId,
            layer.ReceiptSourceId,
            layer.LayananId,
            layer.UnitValuation.AmountPerUnit,
            layer.ExpirationDate,
            layer.Batch,
            boundAt);
}
