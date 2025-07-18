using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record AgamaType : IAgamaKey
{
    public AgamaType(string agamaId, string agamaName)
    {
        Guard.Against.NullOrWhiteSpace(agamaId, nameof(agamaId));
        Guard.Against.NullOrWhiteSpace(agamaName, nameof(agamaName));

        AgamaId = agamaId;
        AgamaName = agamaName;
    }
    
    public string AgamaId { get; init; }
    public string AgamaName { get; init; }
    
    public static AgamaType Default => new("-", "-");
    public static IAgamaKey Key(string id) => Default with { AgamaId = id };
}

public interface IAgamaKey
{
    string AgamaId {get;}
}