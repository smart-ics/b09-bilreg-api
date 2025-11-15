using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record GroupKomponenType : IGroupKomponenKey
{
    #region CREATION
    public GroupKomponenType(string groupKomponenId, string groupKomponenName)
    {
        Guard.Against.NullOrWhiteSpace(groupKomponenId, nameof(groupKomponenId));
        Guard.Against.NullOrWhiteSpace(groupKomponenName, nameof(groupKomponenName));

        GroupKomponenId = groupKomponenId;
        GroupKomponenName = groupKomponenName;
    }
    public static GroupKomponenType Default => new("-", "-");
    public static IGroupKomponenKey Key(string id) => Default with { GroupKomponenId = id };
    #endregion    
    
    public string GroupKomponenId { get; init; }
    public string GroupKomponenName { get; init; }
    
}

public interface IGroupKomponenKey
{
    string GroupKomponenId {get;}
}