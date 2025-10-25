using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.LayananFeature;

public record InstalasiDkType : IInstalasiDkKey
{
    public InstalasiDkType(string instalasiDkId, string instalasiDkName)
    {
        Guard.Against.NullOrWhiteSpace(instalasiDkId, nameof(instalasiDkId));
        Guard.Against.NullOrWhiteSpace(instalasiDkName, nameof(instalasiDkName));

        InstalasiDkId = instalasiDkId;
        InstalasiDkName = instalasiDkName;
    }
    
    public string InstalasiDkId { get; init; }
    public string InstalasiDkName { get; init; }
    
    public static IInstalasiDkKey Key(string id) => new InstalasiDkType(id, "-");
    public static InstalasiDkType Default => new("-", "-");
}

public interface IInstalasiDkKey
{
    string InstalasiDkId {get;}
}