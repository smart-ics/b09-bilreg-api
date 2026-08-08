namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — Capability gate for Native Stock Transfer stock consequence.
/// Default <see cref="Enabled"/> is <c>false</c>; production must not silently enable.
/// Independent from <see cref="StockLedgerDoReceiptOptions"/>.
/// Tests compose options manually; no production HTTP write endpoint in Phase 5.
/// </summary>
public class StockLedgerStockTransferOptions
{
    public const string SECTION_NAME = "StockLedgerStockTransfer";

    /// <summary>
    /// When false, <c>PostStockTransferStockConsequenceHandler</c> returns Disabled without writes.
    /// </summary>
    public bool Enabled { get; set; }
}
