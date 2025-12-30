using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanModel : ITindakanKey
{
    private readonly List<TindakanKomponenType> _listKomponen;

    #region CREATION
    public TindakanModel(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenType> listKomponen, 
        AuditTrailType auditTrail)
    {
        TindakanId = tindakanId;
        TindakanDate = tindakanDate;
        OrderTindakanId = orderTindakanId;
        
        Reg = reg;
        Layanan = layanan;
        
        TipeTarif = tipeTarif;
        Tarif = tarif;
        _listKomponen = listKomponen.ToList() ?? [];

        AuditTrail = auditTrail;
    }

    public static TindakanModel Create(RegModel reg, LayananType layanan,
        NilaiTarifType nilaiTarif, Dictionary<KomponenType, PpaType> listPpa, 
        string userId)
    {
        var newId = Ulid.NewUlid().ToString();

        var listKomp = GenListKomponen(nilaiTarif, listPpa);
        var audit = AuditTrailType.Create(userId, DateTime.Now);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, DateTime.Now, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.TipeTarif,
            tarif, listKomp, audit);
        return result;
    }
    public static TindakanModel CreateByOrder(OrderTdkModel orderTdk, 
        NilaiTarifType nilaiTarif, Dictionary<KomponenType, PpaType> listPpa, string userId)
    {
        var newId = Ulid.NewUlid().ToString();
        if (nilaiTarif.TarifId != orderTdk.Tarif.TarifId)
            throw new Exception("Nilai Tarif tidak sesuai Order Tindakan");

        var listKomp = GenListKomponen(nilaiTarif, listPpa);
        var audit = AuditTrailType.Create(userId, DateTime.Now);
        
        var result = new TindakanModel(newId, DateTime.Now, orderTdk.OrderTdkId,
            orderTdk.Reg, orderTdk.Layanan, nilaiTarif.TipeTarif,
            orderTdk.Tarif, listKomp, audit);
        return result;
    }

    private static IEnumerable<TindakanKomponenType> GenListKomponen(
        NilaiTarifType nilaiTarif, Dictionary<KomponenType, PpaType> listPpa)
    {
        var listKomp = nilaiTarif.ListKomponen
            .Select((x, y) => TindakanKomponenType.Create(y, 
                listPpa.FirstOrDefault(z => z.Key.KomponenId == x.Komponen.KomponenId).Key, 
                listPpa.FirstOrDefault(z => z.Key.KomponenId == x.Komponen.KomponenId).Value ?? PpaType.Default, 
                x.Nilai));
        return listKomp;
    }
    
    public static TindakanModel Default => new(
        "-", 
        DateTime.Today, 
        "", 
        RegModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        TipeTarifType.Default.ToReff(), 
        TarifType.Default.ToReff(),
        [], 
        AuditTrailType.Default
    );

    public static ITindakanKey Key(string id) => Default with { TindakanId = id };
    #endregion

    #region PROPERTIES
    public string TindakanId { get; init; }
    public DateTime TindakanDate { get; init; }
    public string OrderTindakanId { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public TipeTarifReff TipeTarif { get; init; }
    public TarifReff Tarif { get; private set; }
    public decimal Total => _listKomponen.Sum(t => t.SubTotal);
    public IEnumerable<TindakanKomponenType> ListKomponen => _listKomponen;
    public AuditTrailType AuditTrail { get; init; }
    #endregion

    #region BEHAVIOR
    public void Void(string userId)
    {
        AuditTrail.Batal(userId, DateTime.Now);
    }
    #endregion
}

public interface ITindakanKey
{
    string TindakanId { get; }
}

public record TindakanView(string TindakanId, DateTime TindakanDate, string OrderTdkId,
    RegReff Reg, LayananReff Layanan, TarifReff Tarif) : ITindakanKey;
