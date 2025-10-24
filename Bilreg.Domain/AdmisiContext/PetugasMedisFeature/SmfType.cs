namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

public record SmfType : ISmfKey
{
    public SmfType(string smfId, string smfName)
    {
        SmfId = smfId;
        SmfName = smfName;
    }
    
    public string SmfId { get; init; }
    public string SmfName { get; init; }
    
    public static ISmfKey Key(string id) => new SmfType(id, "-");
    public static SmfType Default => new("-", "-");
}

public interface ISmfKey
{
    string SmfId {get;}
}