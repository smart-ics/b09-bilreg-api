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
        string pakaiBedId = "",
        int qty = 1)
    {
        TindakanId = tindakanId;
        TindakanDate = tindakanDate;
        OrderTindakanId = orderTindakanId;
        
        Reg = reg;
        PakaiBedId = pakaiBedId;
        Layanan = layanan;

        Kelas = kelas;
        TipeTarif = tipeTarif;
        Tarif = tarif;
        Qty = qty;
        _listKomponen = listKomponen?.ToList() ?? [];

        AuditTrail = auditTrail;
    }

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1)
    {
        if (qty < 1)
            throw new ArgumentException("Qty must be at least 1", nameof(qty));

        var newId = NunaId.New(ID_PREFIX);

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, pakaiBedId, qty);
        return result;
    }

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, string pakaiBedId, int qty = 1, DateTime occurredAt = default)
        => Create(reg, layanan, nilaiTarif, listKomponenPpaView, userId, occurredAt, pakaiBedId, qty);
    
    public static TindakanModel Save(string tindakanId, RegModel reg,
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView,
        AuditTrailType auditTrail, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1)
    {
        if (qty < 1)
            throw new ArgumentException("Qty must be at least 1", nameof(qty));

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);

        var result = new TindakanModel(tindakanId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, auditTrail, pakaiBedId, qty);
        return result;
    }

    public static TindakanModel Save(string tindakanId, RegModel reg,
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView,
        AuditTrailType auditTrail, string pakaiBedId, int qty = 1, DateTime occurredAt = default)
        => Save(tindakanId, reg, layanan, nilaiTarif, listKomponenPpaView, auditTrail, occurredAt, pakaiBedId, qty);

    public static TindakanModel FromReg(RegModel reg, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1)
    {
        if (qty < 1)
            throw new ArgumentException("Qty must be at least 1", nameof(qty));

        var newId = NunaId.New(ID_PREFIX);

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), reg.Layanan, nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, pakaiBedId, qty);
        return result;
    }

    public static TindakanModel FromReg(RegModel reg, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, string pakaiBedId, int qty = 1, DateTime occurredAt = default)
        => FromReg(reg, nilaiTarif, listKomponenPpaView, userId, occurredAt, pakaiBedId, qty);

    public static TindakanModel Load(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan, KelasReff kelas,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenBase> listKomponen, 
        AuditTrailType auditTrail,
        string pakaiBedId = "",
        int qty = 1)
        => new(tindakanId, tindakanDate, orderTindakanId, reg, layanan, kelas,
            tipeTarif, tarif, listKomponen, auditTrail, pakaiBedId, qty);

    private static IEnumerable<TindakanKomponenBase> GenListKomponen(
        NilaiTarifType nilaiTarif, IEnumerable<KomponenPpaView> listKomponenPpaView, int qty = 1)
    {
        var result = new List<TindakanKomponenBase>();
        var listKompPpaFetched = listKomponenPpaView?.ToList() ?? [];
        var index = 0;
        
        foreach (var item in nilaiTarif.ListKomponen)
        {
            var kompPPa = listKompPpaFetched
                .FirstOrDefault(x => x.Komponen.ToReff() == item.Komponen);
            
            TindakanKomponenBase newItem = kompPPa is null 
                ? TindakanKomponenWithoutPpaType.Create(item.Komponen, index++, item.Nilai, qty) 
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
        "",
        1
    );

    public static ITindakanKey Key(string id) => Default with { TindakanId = id };
    #endregion

    #region PROPERTIES
    public string TindakanId { get; init; }
    public DateTime TindakanDate { get; init; }
    public string OrderTindakanId { get; init; }
    public RegReff Reg { get; init; }
    public string PakaiBedId { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public TipeTarifReff TipeTarif { get; init; }
    public TarifReff Tarif { get; private set; }
    public int Qty { get; init; }
    public decimal Total => _listKomponen.Sum(t => t.SubTotal);
    public IEnumerable<TindakanKomponenBase> ListKomponen => _listKomponen;
    public AuditTrailType AuditTrail { get; init; }
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
    RegReff Reg, LayananReff Layanan, TarifReff Tarif, string PakaiBedId = "") : ITindakanKey;

public record TindakanJualView(string TransaksiId, DateTime TransaksiDate, string OrderTrasaksi, 
    RegReff Reg, LayananReff Layanan, string DiskripsiId, string DiskripsiName, 
    int Qty, decimal Total, string Tipe, string PakaiBedId = "");
