using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

public record SatTugasType : ISatTugasKey
{
    #region CREATION
    public SatTugasType(string satTugasId, string satTugasName, bool isMedis)
    {
        SatTugasId = satTugasId;
        SatTugasName = satTugasName;
        IsMedis = isMedis;
    }
    public static SatTugasType Create(string satTugasId, string satTugasName, bool isMedis)
    {
        Guard.Against.NullOrWhiteSpace(satTugasId);
        Guard.Against.NullOrWhiteSpace(satTugasName);
        return new SatTugasType(satTugasId, satTugasName, isMedis);
    }
    public static SatTugasType Default => new("-", "-", false);
    public static ISatTugasKey Key(string id) => Default with { SatTugasId = id };
    #endregion
    
    #region PROPERTIES
    public string SatTugasId { get; init; }
    public string SatTugasName { get; init; }
    public bool IsMedis { get; init; }
    #endregion
    
    #region BEHAVIOUR
    public SatTugasReff ToReff() => new(SatTugasId, SatTugasName);
    #endregion
    
}

public interface ISatTugasKey
{
    string SatTugasId {get;}
}

public record SatTugasReff(string SatTugasId, string SatTugasName);