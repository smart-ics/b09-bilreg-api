namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Synchronization freshness for an Item + Receipt Source scope relative to Legacy Stock Authority.
/// Orthogonal to <see cref="StockFactOriginEnum"/> and not an authority state.
/// </summary>
public enum SynchronizationStateEnum
{
    Current = 1,
    LegacyChangePending = 2,
    SynchronizationRequired = 3,
    Inconsistent = 4
}
