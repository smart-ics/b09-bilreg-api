using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext;

namespace Bilreg.Domain.AdmisiContext.LayananSub;

public record InstalasiType : IInstalasiKey
{
    public InstalasiType(string instalasiId, string instalasiName, InstalasiDkType instalasiDk)
    {
        Guard.Against.NullOrWhiteSpace(instalasiId, nameof(instalasiId));
        Guard.Against.NullOrWhiteSpace(instalasiName, nameof(instalasiName));
        Guard.Against.Null(instalasiDk, nameof(instalasiDk));

        InstalasiId = instalasiId;
        InstalasiName = instalasiName;
        InstalasiDk = instalasiDk;
    }
    
    public string InstalasiId { get; init; }
    public string InstalasiName { get; init; }
    public InstalasiDkType InstalasiDk { get; init; }
    
    public InstalasiReff ToReff() => new(InstalasiId, InstalasiName);
    
    public static InstalasiType Default => new("-", "-", InstalasiDkType.Default);
    public static IInstalasiKey Key(string id) => Default with { InstalasiId = id };
}

public interface IInstalasiKey
{
    string InstalasiId {get;}
}

public record InstalasiReff(string InstalasiId, string InstalasiName);