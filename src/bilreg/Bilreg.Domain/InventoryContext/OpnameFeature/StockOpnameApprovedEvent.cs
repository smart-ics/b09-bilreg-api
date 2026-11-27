namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public record StockOpnameApprovedEvent(
    string SessionId,
    string LayananId,
    string ApprovedBy,
    DateTime ApprovedAt,
    IReadOnlyList<StockOpnameApprovedItem> Items);

public record StockOpnameApprovedItem(
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal AdjustmentQty,
    decimal Hpp,
    string SatuanId);
