using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record GroupObatDkType : IGroupObatDkKey
{
    #region CREATION
    public GroupObatDkType(string groupObatDkId, string groupObatDkName)
    {
        GroupObatDkId = groupObatDkId;
        GroupObatDkName = groupObatDkName;
    }

    public static GroupObatDkType Create(string groupObatDkId, string groupObatDkName)
    {
        Guard.Against.NullOrWhiteSpace(groupObatDkId, nameof(groupObatDkId));
        Guard.Against.NullOrWhiteSpace(groupObatDkName, nameof(groupObatDkName));
        return new GroupObatDkType(groupObatDkId, groupObatDkName);
    }

    public static GroupObatDkType Default => new("-", "-");

    public static IGroupObatDkKey Key(string id) => Default with { GroupObatDkId = id };
    #endregion

    #region PROPERTIES
    public string GroupObatDkId { get; init; }
    public string GroupObatDkName { get; init; }
    #endregion
}

public interface IGroupObatDkKey
{
    string GroupObatDkId { get; }
}