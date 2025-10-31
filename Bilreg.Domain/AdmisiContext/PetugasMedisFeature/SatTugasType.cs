using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

public record SatTugasType : ISatTugasKey
{
    public SatTugasType(string satTugasId, string satTugasName, bool isMedis)
    {
        Guard.Against.NullOrWhiteSpace(satTugasId, nameof(satTugasId));
        Guard.Against.NullOrWhiteSpace(satTugasName, nameof(satTugasName));

        SatTugasId = satTugasId;
        SatTugasName = satTugasName;
        IsMedis = isMedis;
    }
    
    public string SatTugasId { get; init; }
    public string SatTugasName { get; init; }
    public bool IsMedis { get; init; }
    
    public static SatTugasType Default => new("-", "-", false);
    public static ISatTugasKey Key(string id) => Default with { SatTugasId = id };
}

public interface ISatTugasKey
{
    string SatTugasId {get;}
}