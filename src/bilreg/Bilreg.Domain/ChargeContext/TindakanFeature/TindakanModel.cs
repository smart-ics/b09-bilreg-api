using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanModel : ITindakanKey
{
    private readonly List<TindakanKomponenBase> _listKomponen;
    private const string ID_PREFIX = "TDK";
    #region CREATION
    public TindakanModel(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan, KelasReff kelas,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenBase> listKomponen, 
        AuditTrailType auditTrail,
        int qty = 1)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1");

        TindakanId = tindakanId;
        TindakanDate = tindakanDate;
        OrderTindakanId = orderTindakanId;
        
        Reg = reg;
        Layanan = layanan;

        Kelas = kelas;
        TipeTarif = tipeTarif;
        Tarif = tarif;
        _listKomponen = listKomponen.ToList() ?? [];

        AuditTrail = auditTrail;
        Qty = qty;
    }

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        int qty = 1)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1");

        var newId = NunaId.New(ID_PREFIX);

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, qty);
        return result;
    }
    
    public static TindakanModel Save(string tindakanId, RegModel reg,
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView,
        AuditTrailType auditTrail, DateTime occurredAt = default,
        int qty = 1)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1");

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);

        var result = new TindakanModel(tindakanId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, auditTrail, qty);
        return result;
    }

    public static TindakanModel FromReg(RegModel reg, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        int qty = 1)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1");

        var newId = NunaId.New(ID_PREFIX);

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), reg.Layanan, nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, qty);
        return result;
    }

    private static IEnumerable<TindakanKomponenBase> GenListKomponen(
        NilaiTarifType nilaiTarif, IEnumerable<KomponenPpaView> listKomponenPpaView,
        int qty = 1)
    {
        var result = new List<TindakanKomponenBase>();
        var listKompPpaFetched = listKomponenPpaView.ToList();
        var index = 0;
        
        foreach (var item in nilaiTarif.ListKomponen)
        {
            var kompPPa = listKompPpaFetched
                .FirstOrDefault(x => x.Komponen.ToReff() == item.Komponen);
            
            TindakanKomponenBase newItem = kompPPa is null 
                ? new TindakanKomponenWithoutPpaType(item.Komponen, index++, item.Nilai, qty, item.Nilai * qty) 
                : TindakanKomponenWithPpaType.Create(kompPPa.Komponen, kompPPa.Ppa, 
                    index++, item.Nilai, qty);
            
            result.Add(newItem);
        }
        
        return result.AsEnumerable();
    }
    
    public static TindakanModel Default => new(
        "-", 
        new DateTime(3000, 1, 1),
        "", 
        RegModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        KelasType.Default.ToReff(),
        TipeTarifType.Default.ToReff(), 
        TarifType.Default.ToReff(),
        [], 
        AuditTrailType.Default,
        1
    );

    public static ITindakanKey Key(string id) => Default with { TindakanId = id };
    #endregion

    #region PROPERTIES
    public string TindakanId { get; init; }
    public DateTime TindakanDate { get; init; }
    public string OrderTindakanId { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public TipeTarifReff TipeTarif { get; init; }
    public TarifReff Tarif { get; private set; }
    public int Qty { get; init; } = 1;
    public decimal Total => _listKomponen.Sum(t => t.SubTotal);
    public IEnumerable<TindakanKomponenBase> ListKomponen => _listKomponen;
    public AuditTrailType AuditTrail { get; init; }
    public bool IsVoid => AuditTrail.IsVoided;
    #endregion

    #region BEHAVIOR
    public void Void(string userId, DateTime voidedAt = default)
    {
        AuditTrail.Batal(userId, voidedAt);
    }
    #endregion
}

public interface ITindakanKey
{
    string TindakanId { get; }
}

public record TindakanView(string TindakanId, DateTime TindakanDate, string OrderTdkId,
    RegReff Reg, LayananReff Layanan, TarifReff Tarif) : ITindakanKey;

public record TindakanJualView(string TransaksiId, DateTime TransaksiDate, string OrderTrasaksi, 
    RegReff Reg, LayananReff Layanan, string DiskripsiId, string DiskripsiName, 
    int Qty, decimal Total, string Tipe);
