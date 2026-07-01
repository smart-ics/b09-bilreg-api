using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record BentukType : IBentukKey
{
    #region CREATION
    public BentukType(string bentukId, string bentukName)
    {
        BentukId = bentukId;
        BentukName = bentukName;
    }

    public static BentukType Create(string bentukId, string bentukName)
    {
        Guard.Against.NullOrWhiteSpace(bentukId, nameof(bentukId));
        Guard.Against.NullOrWhiteSpace(bentukName, nameof(bentukName));
        return new BentukType(bentukId, bentukName);
    }

    public static BentukType Default => new("-", "-");

    public static IBentukKey Key(string id) => Default with { BentukId = id };
    #endregion

    #region PROPERTIES
    public string BentukId { get; init; }
    public string BentukName { get; init; }
    #endregion
}

public interface IBentukKey
{
    string BentukId { get; }
}