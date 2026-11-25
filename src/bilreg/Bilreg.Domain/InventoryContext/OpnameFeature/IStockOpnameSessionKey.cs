namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public interface IStockOpnameSessionKey
{
    string SessionId { get; }
}

public record StockOpnameSessionKey(string SessionId) : IStockOpnameSessionKey;
