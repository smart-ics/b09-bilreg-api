using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KelasTerapiType : IKelasTerapiKey
{
    #region CREATION
    public KelasTerapiType(string kelasTerapiId, string kelasTerapiName)
    {
        KelasTerapiId = kelasTerapiId;
        KelasTerapiName = kelasTerapiName;
    }

    public static KelasTerapiType Create(string kelasTerapiId, string kelasTerapiName)
    {
        Guard.Against.NullOrWhiteSpace(kelasTerapiId, nameof(kelasTerapiId));
        Guard.Against.NullOrWhiteSpace(kelasTerapiName, nameof(kelasTerapiName));
        return new KelasTerapiType(kelasTerapiId, kelasTerapiName);
    }

    public static KelasTerapiType Default => new("-", "-");

    public static IKelasTerapiKey Key(string id) => Default with { KelasTerapiId = id };
    #endregion

    #region PROPERTIES
    public string KelasTerapiId { get; init; }
    public string KelasTerapiName { get; init; }
    #endregion
}

public interface IKelasTerapiKey
{
    string KelasTerapiId { get; }
}