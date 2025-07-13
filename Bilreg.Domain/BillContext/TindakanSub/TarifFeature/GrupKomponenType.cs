using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record GroupKomponenType : IGroupKomponenKey
{
    public GroupKomponenType(string groupKomponenId, string groupKomponenName)
    {
        Guard.Against.NullOrWhiteSpace(groupKomponenId, nameof(groupKomponenId));
        Guard.Against.NullOrWhiteSpace(groupKomponenName, nameof(groupKomponenName));

        GroupKomponenId = groupKomponenId;
        GroupKomponenName = groupKomponenName;
    }
    
    public string GroupKomponenId { get; init; }
    public string GroupKomponenName { get; init; }
    
    public static GroupKomponenType Default => new("-", "-");
    public static IGroupKomponenKey Key(string id) => Default with { GroupKomponenId = id };
}

public interface IGroupKomponenKey
{
    string GroupKomponenId {get;}
}