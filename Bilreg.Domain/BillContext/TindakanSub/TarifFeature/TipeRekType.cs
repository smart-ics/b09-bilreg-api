using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record TipeRekType : ITipeRekKey
{
    public TipeRekType(string tipeRekId, string tipeRekName)
    {
        Guard.Against.NullOrWhiteSpace(tipeRekId, nameof(tipeRekId));
        Guard.Against.NullOrWhiteSpace(tipeRekName, nameof(tipeRekName));

        TipeRekId = tipeRekId;
        TipeRekName = tipeRekName;
    }
    
    public string TipeRekId { get; init; }
    public string TipeRekName { get; init; }
    
    public static TipeRekType Default => new("-", "-");
    public static ITipeRekKey Key(string id) => Default with { TipeRekId = id };
}

public interface ITipeRekKey
{
    string TipeRekId {get;}
}