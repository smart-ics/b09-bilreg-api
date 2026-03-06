using Ardalis.GuardClauses;
using Bilreg.Domain.AccountingContext.UnitFeature;

namespace Bilreg.Domain.AdmisiContext.LayananFeature;

public record LayananType : ILayananKey
{
    #region CREATION
    public LayananType(string layananId, string layananName, bool isAKtif,
        InstalasiReff instalasi, LayananDkReff layananDk, 
        TipeLayananDkType tipeLayananDk, InstalasiDkType instalasiDk, 
        UnitReff unitPcc, PoliBpjsReff layananBpjs)
    {
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(layananName, nameof(layananName));
        Guard.Against.Null(instalasi, nameof(instalasi));
        Guard.Against.Null(layananDk, nameof(layananDk));
        Guard.Against.Null(tipeLayananDk, nameof(tipeLayananDk));
        Guard.Against.Null(instalasiDk, nameof(instalasiDk));

        LayananId = layananId;
        LayananName = layananName;
        IsAktif = isAKtif;
        Instalasi = instalasi;
        LayananDk = layananDk;
        TipeLayananDk = tipeLayananDk;
        InstalasiDk = instalasiDk;
        UnitPcc = unitPcc;
        PoliBpjs = layananBpjs;
    }
    public static LayananType Default => new("-", "-", true,
        InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(), 
        TipeLayananDkType.Default, InstalasiDkType.Default,
        UnitType.Default.ToReff(), new PoliBpjsReff("-", "-"));

    public static ILayananKey Key(string id) => Default with { LayananId = id };
    #endregion
    
    #region PROPERTIES
    public string LayananId { get; init; }
    public string LayananName { get; init; }
    public bool IsAktif { get; init; }
    
    public InstalasiReff Instalasi { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public TipeLayananDkType TipeLayananDk { get; init; }
    public InstalasiDkType InstalasiDk { get; init; }
    public UnitReff UnitPcc { get; init; }
    public PoliBpjsReff PoliBpjs { get; init;  }
    
    #endregion
    
    public LayananReff ToReff() => new(LayananId, LayananName);
}

public interface ILayananKey
{
    string LayananId {get;}
}

public record LayananReff(string LayananId, string LayananName) : ILayananKey;

public record PoliBpjsReff(string PoliBpjsId, string PoliBpjsName);