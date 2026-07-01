using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record NegaraType : INegaraKey
{
    public NegaraType(string negaraId, string negaraName, string negaraEngName, 
        string alpha3, string negaraNumeric)
    {
        Guard.Against.NullOrWhiteSpace(negaraId, nameof(negaraId));
        Guard.Against.NullOrWhiteSpace(negaraName, nameof(negaraName));

        NegaraId = negaraId;
        NegaraName = negaraName;
        NegaraEngName = negaraEngName;
        Alpha3 = alpha3;
        NegaraNumeric = negaraNumeric;
    }
    public string NegaraId { get; init; }
    public string NegaraName { get; init; }
    public string NegaraEngName { get; init; }
    public string Alpha3 { get; init; }
    public string NegaraNumeric { get; init; }

    public static NegaraType Default => new NegaraType("-", "-", "-", "-", "-");
    public static INegaraKey Key(string id) => Default with { NegaraId = id };
}

public interface INegaraKey
{
    string NegaraId { get;}
}
