using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record GroupTarifDkType : IGroupTarifDkKey
{
    public GroupTarifDkType(string groupTarifDkId, string groupTarifDkName)
    {
        Guard.Against.NullOrWhiteSpace(groupTarifDkId, nameof(groupTarifDkId));
        Guard.Against.NullOrWhiteSpace(groupTarifDkName, nameof(groupTarifDkName));

        GroupTarifDkId = groupTarifDkId;
        GroupTarifDkName = groupTarifDkName;
    }
    
    public string GroupTarifDkId { get; init; }
    public string GroupTarifDkName { get; init; }
    
    public static GroupTarifDkType Default => new("-", "-");
    public static IGroupTarifDkKey Key(string id) => Default with { GroupTarifDkId = id };
}

public interface IGroupTarifDkKey
{
    string GroupTarifDkId {get;}
}