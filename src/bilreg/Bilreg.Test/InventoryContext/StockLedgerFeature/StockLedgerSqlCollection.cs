namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// Serializes dual-write / live SQL tests that share <c>tb_buku</c>/<c>tb_stok</c> and can deadlock under parallel xUnit.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class StockLedgerSqlCollection
{
    public const string Name = "StockLedgerSql";
}
