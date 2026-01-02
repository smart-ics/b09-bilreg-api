using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillingType : ITrsBillingKey
{
    private readonly List<ITrsBilling2> _listTrsBilling2 = [];
    #region CREATION
    public TrsBillingType(string billingId, int modul, DateTime tglTrs, 
        RegReff reg, LayananReff layanan, KelasReff kelas, 
        AuditInfoType auditInfo, decimal subTotal, decimal diskon, 
        decimal tax, decimal biaya, RekapCetakReff rekapCetak, 
        TrsBillKetType keterangan, IEnumerable<ITrsBilling2> listTrsBilling2)
    {
        TrsBillingId = billingId;
        Modul = modul;
        TglTrs = tglTrs;
        Reg = reg;
        Layanan = layanan;
        Kelas = kelas;
        AuditInfo = auditInfo;
        SubTotal = subTotal;
        Diskon = diskon;
        Tax = tax;
        Biaya = biaya;
        RekapCetak = rekapCetak;
        Keterangan = keterangan;
        _listTrsBilling2 = listTrsBilling2.ToList();
    }

    public static TrsBillingType Create(string trsId, int modul, DateTime tglTrs, 
        RegReff reg, LayananReff layanan, KelasReff kelas, 
        AuditInfoType auditInfo, decimal subTotal, decimal diskon, 
        decimal tax, decimal biaya, RekapCetakReff rekapCetak, 
        TrsBillKetType keterangan)
    {
        return new TrsBillingType(trsId, modul, tglTrs, reg, layanan, kelas, 
            auditInfo, subTotal, diskon, tax, biaya, rekapCetak, keterangan, []);
    }

    public static TrsBillingType Default => new("-", 0, DateTime.MinValue, 
        RegModel.Default.ToReff(), LayananType.Default.ToReff(), 
        KelasType.Default.ToReff(), 
        AuditInfoType.Default, 0, 0, 0, 0, RekapCetakType.Default.ToReff(), 
        TrsBillKetType.Default, []);

    public static ITrsBillingKey Key(string id) => Default with { TrsBillingId = id };
    #endregion

    #region PROPERTIES
    public string TrsBillingId { get; init; }
    public int Modul { get; init; }
    public DateTime TglTrs { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public decimal SubTotal { get; init; }
    public decimal Diskon { get; init; }
    public decimal Tax { get; init; }
    public decimal Biaya { get; init; }
    public decimal Total => SubTotal - Diskon + Tax + Biaya;
    public TrsBillKetType Keterangan { get; init; }
    public IEnumerable<ITrsBilling2> ListTrsBilling2 => _listTrsBilling2;
    #endregion
}

public interface ITrsBillingKey
{
    string TrsBillingId { get; }
}

public record TrsBillKetType(string Keterangan, string Keterangan2, string RefBiaya, int Qty, string TrsMainId)
{
    public static TrsBillKetType Default => new("", "", "", 0, "");
}

