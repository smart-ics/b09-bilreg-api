namespace Bilreg.Domain.BedUsageContext.WardFeature;

public record KelasDkType(string KelasDkId, string KelasDkName): IKelasDkKey
{
    public static KelasDkType Default => new KelasDkType("-", "-");
    public static IKelasDkKey Key(string id) => new KelasDkType(id, "-");
}

public interface IKelasDkKey
{
    string KelasDkId { get; }
}