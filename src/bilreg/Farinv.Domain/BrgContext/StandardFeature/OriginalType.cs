using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record OriginalType : IOriginalKey
{
    #region CREATION
    public OriginalType(string originalId, string originalName)
    {
        OriginalId = originalId;
        OriginalName = originalName;
    }

    public static OriginalType Create(string originalId, string originalName)
    {
        Guard.Against.NullOrWhiteSpace(originalId, nameof(originalId));
        Guard.Against.NullOrWhiteSpace(originalName, nameof(originalName));
        return new OriginalType(originalId, originalName);
    }

    public static OriginalType Default => new("-", "-");

    public static IOriginalKey Key(string id) => Default with { OriginalId = id };
    #endregion

    #region PROPERTIES
    public string OriginalId { get; init; }
    public string OriginalName { get; init; }
    #endregion
}

public interface IOriginalKey
{
    string OriginalId { get; }
}