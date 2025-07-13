using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record PropinsiType : IPropinsiKey
{
    public PropinsiType(string propinsiId, string propinsiName)
    {
        Guard.Against.NullOrWhiteSpace(propinsiId, nameof(propinsiId));
        Guard.Against.NullOrWhiteSpace(propinsiName, nameof(propinsiName));

        PropinsiId = propinsiId;
        PropinsiName = propinsiName;
    }
    
    public string PropinsiId { get; init; }
    public string PropinsiName { get; init; }
    
    public static IPropinsiKey Key(string id) => new PropinsiType(id, "-");
    public static PropinsiType Default => new("-", "-");
}

public interface IPropinsiKey
{
    string PropinsiId {get;}
}