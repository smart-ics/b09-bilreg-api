namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Natural uniqueness key for a Stock Batch: Item + Receipt Source (DO).
/// </summary>
public record StockBatchKeyType(string BrgId, string BrgMasukReffId);
