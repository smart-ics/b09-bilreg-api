namespace Bilreg.Domain.AdmisiContext.PpaFeature;

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
    public static GroupSpesialisType Bedah => new("BDH", "Bedah");
    public static GroupSpesialisType Obgyn => new("OBG", "Obgyn");
    public static GroupSpesialisType Anestesi => new("ANE", "Anestesi");
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