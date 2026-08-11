namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// GAP-STL-001 safe interim: config section <c>StockLedger</c>, property
/// <c>CoexistenceEnabled</c> → key <c>StockLedger:CoexistenceEnabled</c>.
/// </summary>
public sealed class StockLedgerCoexistenceOptions
{
    public const string SectionName = "StockLedger";

    /// <summary>
    /// When true: freshness gate + dual-write + side tables required (coexistence).
    /// When false (cutover): gate no-ops; ledger-only writes in later slices (ADR-STL-007).
    /// </summary>
    public bool CoexistenceEnabled { get; set; } = true;
}
