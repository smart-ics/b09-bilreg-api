using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record GroupTarifType : IGroupTarifKey
{
    #region CREATION
    public GroupTarifType(string groupTarifId, string groupTarifName)
    {
        GroupTarifId = groupTarifId;
        GroupTarifName = groupTarifName;
    }
    public static GroupTarifType Create(string groupTarifId, string groupTarifName)
    {
        Guard.Against.NullOrWhiteSpace(groupTarifId);
        Guard.Against.NullOrWhiteSpace(groupTarifName);
        return new GroupTarifType(groupTarifId, groupTarifName);
    }
    public static GroupTarifType Default => new("-", "-");
    public static IGroupTarifKey Key(string id) => Default with { GroupTarifId = id };
    #endregion
    
    #region PROPERTIES
    public string GroupTarifId { get; init; }
    public string GroupTarifName { get; init; }
    #endregion
}

public interface IGroupTarifKey
{
    string GroupTarifId {get;}
}