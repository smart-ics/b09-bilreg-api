using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record PropinsiType : IPropinsiKey
{
    public PropinsiType(string propinsiId, string propinsiName)
    {
        PropinsiId = propinsiId;
        PropinsiName = propinsiName;
    }
    
    public string PropinsiId { get; init; }
    public string PropinsiName { get; init; }
    
    public static PropinsiType Default => new("-", "-");
    public static IPropinsiKey Key(string id) => Default with { PropinsiId = id };
}

public interface IPropinsiKey
{
    string PropinsiId {get;}
}
