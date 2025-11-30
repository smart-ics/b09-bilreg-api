using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record GroupTarifDkType : IGroupTarifDkKey
{
    #region CREATION
    public GroupTarifDkType(string groupTarifDkId, string groupTarifDkName)
    {
        GroupTarifDkId = groupTarifDkId;
        GroupTarifDkName = groupTarifDkName;
    }
    public static GroupTarifDkType Create(string groupTarifDkId, string groupTarifDkName)
    {
        Guard.Against.NullOrWhiteSpace(groupTarifDkId);
        Guard.Against.NullOrWhiteSpace(groupTarifDkName);
        return new GroupTarifDkType(groupTarifDkId, groupTarifDkName);
    }
    public static GroupTarifDkType Default => new("-", "-");
    public static IGroupTarifDkKey Key(string id) => Default with { GroupTarifDkId = id };
    #endregion
    
    #region PROPERTIES
    public string GroupTarifDkId { get; init; }
    public string GroupTarifDkName { get; init; }
    #endregion
}

public interface IGroupTarifDkKey
{
    string GroupTarifDkId {get;}
}