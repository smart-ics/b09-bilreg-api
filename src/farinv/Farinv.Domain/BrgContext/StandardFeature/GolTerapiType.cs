using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record GolTerapiType : IGolTerapiKey
{
    #region CREATION
    public GolTerapiType(string golTerapiId, string golTerapiName)
    {
        GolTerapiId = golTerapiId;
        GolTerapiName = golTerapiName;
    }

    public static GolTerapiType Create(string golTerapiId, string golTerapiName)
    {
        Guard.Against.NullOrWhiteSpace(golTerapiId, nameof(golTerapiId));
        Guard.Against.NullOrWhiteSpace(golTerapiName, nameof(golTerapiName));
        return new GolTerapiType(golTerapiId, golTerapiName);
    }

    public static GolTerapiType Default => new("-", "-");

    public static IGolTerapiKey Key(string id) => Default with { GolTerapiId = id };
    #endregion

    #region PROPERTIES
    public string GolTerapiId { get; init; }
    public string GolTerapiName { get; init; }
    #endregion
}

public interface IGolTerapiKey
{
    string GolTerapiId { get; }
}