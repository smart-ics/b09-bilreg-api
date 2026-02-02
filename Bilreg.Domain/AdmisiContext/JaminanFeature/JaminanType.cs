using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

public record JaminanType : IJaminanKey
{
    #region CREATE
    public JaminanType(string jaminanId, string jaminanName,
        bool isAKtif, AlamatType alamat, CaraBayarDkType caraBayarDk, 
        GroupJaminanReff grupJaminan, 
        JaminanTipeTarifType tipeTarif,
        JaminanRekeningType rekening,
        JaminanTipeBarangType tipeBarang)
    {
        JaminanId = jaminanId;
        JaminanName = jaminanName;
        IsAktif = isAKtif;
        Alamat = alamat;
        CaraBayarDk = caraBayarDk;
        GroupJaminan = grupJaminan;
        Rekening = rekening;
        TipeTarif = tipeTarif;
        TipeBarang = tipeBarang;
    }
    public static JaminanType Default => new("-", "-", true, 
        AlamatType.Default, CaraBayarDkType.Default, 
        GroupJaminanType.Default.ToReff(), 
        JaminanTipeTarifType.Default, 
        JaminanRekeningType.Default,
        JaminanTipeBarangType.Default);
    public static IJaminanKey Key(string id) => Default with { JaminanId = id };
    public static JaminanType Umum => new("000", "Umum", true, 
        AlamatType.Default, CaraBayarDkType.BayarSendiri, 
        GroupJaminanType.Default.ToReff(), 
        JaminanTipeTarifType.Default, 
        JaminanRekeningType.Default,
        JaminanTipeBarangType.Default);
    #endregion
    
    #region PROPERTIES
    public string JaminanId { get; init; }
    public string JaminanName { get; init; }
    public bool IsAktif { get; init; }
    public AlamatType Alamat { get; init; }
    public CaraBayarDkType CaraBayarDk { get; init; }
    public GroupJaminanReff GroupJaminan { get; init; }
    public JaminanRekeningType Rekening { get; init; }
    public JaminanTipeTarifType TipeTarif { get; init; }
    public JaminanTipeBarangType TipeBarang { get; init; }
    #endregion

    public JaminanReff ToReff() => new(JaminanId, JaminanName);
}

public interface IJaminanKey
{
    string JaminanId {get;}
}

public record JaminanReff(string JaminanId, string JaminanName) : IJaminanKey;

public record JaminanRekeningType(
    CoaType PpdpJasaRajal, CoaType PpdpObatRajal,
    CoaType PpdpJasaRanap,CoaType PpdpObatRanap)
{
    public static JaminanRekeningType Default =>
        new JaminanRekeningType(CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default);
};

public record JaminanTipeTarifType(TipeTarifReff Rajal, TipeTarifReff Ranap)
{
    public static JaminanTipeTarifType Default =>
        new JaminanTipeTarifType(TipeTarifType.Default.ToReff(), TipeTarifType.Default.ToReff());
}

public record JaminanTipeBarangType(TipeBrgType Rajal, TipeBrgType Ranap)
{
    public static JaminanTipeBarangType Default =>
        new JaminanTipeBarangType(new TipeBrgType("-", "-"), new TipeBrgType("-", "-"));
}

public record TipeBrgType(string TipeBarangId, string TipeBarangName);