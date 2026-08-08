namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Explicit persistence failure for Stock Ledger foundation tables.
/// Concurrency conflicts never silently overwrite Position state.
/// </summary>
public sealed class StockLedgerPersistenceException : InvalidOperationException
{
    private StockLedgerPersistenceException(string code, string message)
        : base(message) =>
        Code = code;

    public string Code { get; }

    public static StockLedgerPersistenceException Concurrency(string message) =>
        new("CONCURRENCY_CONFLICT", message);

    public static StockLedgerPersistenceException Immutable(string message) =>
        new("IMMUTABLE_CONFLICT", message);

    public static StockLedgerPersistenceException Integrity(string message) =>
        new("INTEGRITY_CONFLICT", message);
}
