using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

public record LayananType : ILayananKey
{
    public LayananType(string layananId, string layananName, bool isAKtif,
        InstalasiReff instalasi, InstalasiDkType instalasiDk,
        LayananDkReff layananDk, TipeLayananDkType tipeLayananDk)
    {
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(layananName, nameof(layananName));
        Guard.Against.Null(instalasi, nameof(instalasi));
        Guard.Against.Null(instalasiDk, nameof(instalasiDk));
        Guard.Against.Null(layananDk, nameof(layananDk));
        Guard.Against.Null(tipeLayananDk, nameof(tipeLayananDk));

        LayananId = layananId;
        LayananName = layananName;
        IsAKtif = isAKtif;
        Instalasi = instalasi;
        InstalasiDk = instalasiDk;
        LayananDk = layananDk;
        TipeLayananDk = tipeLayananDk;
    }
    
    public string LayananId { get; init; }
    public string LayananName { get; init; }
    public bool IsAKtif { get; init; }
    
    public InstalasiReff Instalasi { get; init; }
    public InstalasiDkType InstalasiDk { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public TipeLayananDkType TipeLayananDk { get; init; }
    
    public static LayananType Default => new("-", "-", true,
        InstalasiType.Default.ToReff(), InstalasiDkType.Default, 
        LayananDkType.Default.ToReff(), TipeLayananDkType.Default);
    public static ILayananKey Key(string id) => Default with { LayananId = id };
}

public interface ILayananKey
{
    string LayananId {get;}
}