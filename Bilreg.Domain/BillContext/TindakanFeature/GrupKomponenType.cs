using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;
public record GroupKomponenType : IGroupKomponenKey
{
    #region CREATION
    public GroupKomponenType(string groupKomponenId, string groupKomponenName)
    {
        GroupKomponenId = groupKomponenId;
        GroupKomponenName = groupKomponenName;
    }
    public static GroupKomponenType Create(string groupKomponenId, string groupKomponenName)
    {
        Guard.Against.NullOrWhiteSpace(groupKomponenId, nameof(groupKomponenId));
        Guard.Against.NullOrWhiteSpace(groupKomponenName, nameof(groupKomponenName));
        return new GroupKomponenType(groupKomponenId, groupKomponenName);
    }
    public static GroupKomponenType Default => new("-", "-");
    public static IGroupKomponenKey Key(string id) => Default with { GroupKomponenId = id };
    #endregion
    
    #region PROPERTIES
    public string GroupKomponenId { get; init; }
    public string GroupKomponenName { get; init; }
    #endregion
}

public interface IGroupKomponenKey
{
    string GroupKomponenId {get;}
}