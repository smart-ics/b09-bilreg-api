namespace Farinv.Domain.InventoryContext.StokFeature;

public record TrsReffType(string ReffId, DateTime ReffDate)
{
    public static TrsReffType Default => new TrsReffType("-", new DateTime(3000,1,1));
}