using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record GroupTarifType : IGroupTarifKey
{
    public GroupTarifType(string groupTarifId, string groupTarifName)
    {
        Guard.Against.NullOrWhiteSpace(groupTarifId, nameof(groupTarifId));
        Guard.Against.NullOrWhiteSpace(groupTarifName, nameof(groupTarifName));

        GroupTarifId = groupTarifId;
        GroupTarifName = groupTarifName;
    }
    
    public string GroupTarifId { get; init; }
    public string GroupTarifName { get; init; }
    
    public static GroupTarifType Default => new("-", "-");
    public static IGroupTarifKey Key(string id) => Default with { GroupTarifId = id };
}

public interface IGroupTarifKey
{
    string GroupTarifId {get;}
}