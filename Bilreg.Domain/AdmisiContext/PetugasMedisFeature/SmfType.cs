using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
public record SmfType : ISmfKey
{
    #region CREATION
    public SmfType(string smfId, string smfName)
    {
        SmfId = smfId;
        SmfName = smfName;
    }
    public static SmfType Create(string smfId, string smfName)
    {
        Guard.Against.NullOrWhiteSpace(smfId);
        Guard.Against.NullOrWhiteSpace(smfName);
        return new SmfType(smfId, smfName);
    }
    public static SmfType Default => new("-", "-");
    public static ISmfKey Key(string id) => Default with { SmfId = id };
    #endregion
    
    #region PROPERTIES
    public string SmfId { get; init; }
    public string SmfName { get; init; }
    #endregion
}

public interface ISmfKey
{
    string SmfId {get;}
}