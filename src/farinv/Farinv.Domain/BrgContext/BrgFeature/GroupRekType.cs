using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.BrgFeature;

public record GroupRekType : IGroupRekKey
{
    #region CREATION
    public GroupRekType(string groupRekId, string groupRekName, GroupRekDkType groupRekDk)
    {
        Guard.Against.NullOrWhiteSpace(groupRekId, nameof(groupRekId));
        Guard.Against.NullOrWhiteSpace(groupRekName, nameof(groupRekName));
        Guard.Against.Null(groupRekDk, nameof(groupRekDk));

        GroupRekId = groupRekId;
        GroupRekName = groupRekName;
        GroupRekDk = groupRekDk;
    }

    public static GroupRekType Create(string groupRekId, string groupRekName, GroupRekDkType groupRekDk)
    {
        return new GroupRekType(groupRekId, groupRekName, groupRekDk);
    }

    public static GroupRekType Default => new("-", "-", GroupRekDkType.Default);

    public static IGroupRekKey Key(string id) => Default with { GroupRekId = id };
    #endregion

    #region PROPERTIES
    public string GroupRekId { get; init; }
    public string GroupRekName { get; init; }
    public GroupRekDkType GroupRekDk { get; init; }
    #endregion

    #region BEHAVIOR
    public GroupRekReff ToReff() => new(GroupRekId, GroupRekName);
    #endregion
}

public interface IGroupRekKey
{
    string GroupRekId { get; }
}

public record GroupRekReff(string GroupRekId, string GroupRekName);