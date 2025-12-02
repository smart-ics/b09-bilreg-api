using Ardalis.GuardClauses;
namespace Bilreg.Domain.AdmisiContext.PpaFeature;


public record PpaMapHidokType : IPpaMapHidokKey
{
    #region CREATION
    public PpaMapHidokType(string ppaId, string ppaName, string ppaHidokId)
    {
        PpaId = ppaId;
        PpaName = ppaName;
        PpaHidokId = ppaHidokId;
    }

    public static PpaMapHidokType Create(string ppaId, string ppaName, string ppaHidokId)
    {
        Guard.Against.NullOrWhiteSpace(ppaId, nameof(ppaId));
        Guard.Against.NullOrWhiteSpace(ppaName, nameof(ppaName));
        Guard.Against.NullOrWhiteSpace(ppaHidokId, nameof(ppaHidokId));

        return new PpaMapHidokType(ppaId, ppaName, ppaHidokId);
    }

    public static PpaMapHidokType Default => new("-", "-", "-");
    public static IPpaMapHidokKey Key(string id) => Default with { PpaHidokId = id };
    #endregion

    #region PROPERTIES
    public string PpaHidokId { get; init; }
    public string PpaId { get; init; }
    public string PpaName { get; init; }
    #endregion

    #region BEHAVIOR
    
    #endregion
}

public interface IPpaMapHidokKey
{
    string PpaHidokId { get; }
}
