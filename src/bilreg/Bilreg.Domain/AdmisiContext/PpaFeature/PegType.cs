using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public record PegType : IPegKey
{
    #region CREATION
    public PegType(string pegId, string pegName)
    {
        PegId = pegId;
        PegName = pegName;
    }

    public static PegType Create(string pegId, string pegName)
    {
        return new PegType(pegId, pegName);
    }

    public static PegType Default => new("-", "-");

    public static IPegKey Key(string id) => Default with { PegId = id };
    #endregion

    #region PROPERTIES
    public string PegId { get; init; }
    public string PegName { get; init; }
    #endregion
}

public interface IPegKey
{
    string PegId { get; }
}
