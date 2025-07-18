using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KotaType : IKotaKey
{
    public KotaType(string kotaId, string kotaName)
    {
        Guard.Against.NullOrWhiteSpace(kotaId, nameof(kotaId));
        Guard.Against.NullOrWhiteSpace(kotaName, nameof(kotaName));

        KotaId = kotaId;
        KotaName = kotaName;
    }
    
    public string KotaId { get; init; }
    public string KotaName { get; init; }
    
    public static IKotaKey Key(string id) => new KotaType(id, "-");
    public static KotaType Default => new("-", "-");
}

public interface IKotaKey
{
    string KotaId {get;}
}