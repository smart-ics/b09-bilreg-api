using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanModel : ITindakanKey
{
    private readonly List<TindakanKomponenBase> _listKomponen;

    #region CREATION
    public TindakanModel(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenBase> listKomponen, 
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

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId)
    {
        var newId = Ulid.NewUlid().ToString();

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView);
        var audit = AuditTrailType.Create(userId, DateTime.Now);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, DateTime.Now, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.TipeTarif,
            tarif, listKomp, audit);
        return result;
    }

    private static IEnumerable<TindakanKomponenBase> GenListKomponen(
        NilaiTarifType nilaiTarif, IEnumerable<KomponenPpaView> listKomponenPpaView)
    {
        var result = new List<TindakanKomponenBase>();
        var listKompPpaFetched = listKomponenPpaView.ToList();
        var index = 0;
        
        foreach (var item in nilaiTarif.ListKomponen)
        {
            var kompPPa = listKompPpaFetched
                .FirstOrDefault(x => x.Komponen.ToReff() == item.Komponen);
            
            TindakanKomponenBase newItem = kompPPa is null 
                ? new TindakanKomponenWithoutPpaType(item.Komponen,index++, item.Nilai, 1, item.Nilai) 
                : TindakanKomponenWithPpaType.Create(kompPPa.Komponen, kompPPa.Ppa, 
                    index++, item.Nilai, 1);
            
            result.Add(newItem);
        }
        
        return result.AsEnumerable();
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
    public IEnumerable<TindakanKomponenBase> ListKomponen => _listKomponen;
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
