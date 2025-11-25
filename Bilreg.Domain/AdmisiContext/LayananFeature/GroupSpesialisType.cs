namespace Bilreg.Domain.AdmisiContext.LayananFeature;

public record GroupSpesialisType : IGroupSpesialisKey
{
    #region CREATION
    public GroupSpesialisType(string groupSpesialisId, string groupSpesialisName)
    {
        GroupSpesialisId = groupSpesialisId;
        GroupSpesialisName = groupSpesialisName;
    }

    public static GroupSpesialisType Default => new("-", "-");
    public static IGroupSpesialisKey Key(string id) => Default with { GroupSpesialisId = id };
    #endregion

    #region PROPERTIES
    public string GroupSpesialisId { get; init; }
    public string GroupSpesialisName { get; init; }
    #endregion
}

public interface IGroupSpesialisKey
{
    string GroupSpesialisId { get; }
}