namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S2 — Capability gate for Native DO Receipt stock consequence.
/// Default <see cref="Enabled"/> is <c>false</c>; production must not silently enable.
/// Tests compose options manually; no production HTTP write endpoint in Phase 4.
/// </summary>
public class StockLedgerDoReceiptOptions
{
    public const string SECTION_NAME = "StockLedgerDoReceipt";

    /// <summary>
    /// When false, <c>PostDoReceiptStockConsequenceHandler</c> and
    /// <c>VoidDoReceiptStockConsequenceHandler</c> return Disabled without writes.
    /// </summary>
    public bool Enabled { get; set; }
}
