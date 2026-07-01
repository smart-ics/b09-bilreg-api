using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public record SatTugasType : ISatTugasKey
{
    #region CREATION
    public SatTugasType(string satTugasId, string satTugasName, ProfesiType profesi)
    {
        SatTugasId = satTugasId;
        SatTugasName = satTugasName;
        Profesi = profesi;
    }
    public static SatTugasType Create(string satTugasId, string satTugasName, ProfesiType profesi)
    {
        Guard.Against.NullOrWhiteSpace(satTugasId);
        Guard.Against.NullOrWhiteSpace(satTugasName);
        Guard.Against.Null(profesi);
        return new SatTugasType(satTugasId, satTugasName, profesi);
    }
    public static SatTugasType Default => new("-", "-", ProfesiType.Default);
    public static ISatTugasKey Key(string id) => Default with { SatTugasId = id };
    #endregion
    
    #region PROPERTIES
    public string SatTugasId { get; init; }
    public string SatTugasName { get; init; }
    public bool IsMedis { get; init; }
    public ProfesiType Profesi { get; init; }
    #endregion
}

public interface ISatTugasKey
{
    string SatTugasId {get;}
}
