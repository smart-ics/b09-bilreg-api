namespace Bilreg.Domain.AccountingContext.UnitFeature;

public record MapJaminanJkType
{
    public MapJaminanJkType(string jaminanId, string jkId)
    {
        JaminanId = jaminanId;
        JkId = jkId;
    }
    public string JaminanId { get; init; }
    public string JkId { get; init; }
    public static MapJaminanJkType Default => new(string.Empty, string.Empty);
}
