using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
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

    private static string GenerateNewId()
    {
        var id = NunaId.New(ID_PREFIX);
        return id.Length > 10 ? id[..10] : id;
    }

    #region CREATION
    public TindakanModel(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan, KelasReff kelas,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenBase> listKomponen, 
        AuditTrailType auditTrail,
        string pakaiBedId = "",
        int qty = 1,
        bool isDiskonDokter = false)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1 / Qty must be at least 1");

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
        IsDiskonDokter = isDiskonDokter;
    }

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1,
        bool isDiskonDokter = false,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1 / Qty must be at least 1");

        var newId = GenerateNewId();

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty, isDiskonDokter, reg.Dokter, komponenDiskonMap);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        var hasDoctorDiskon = isDiskonDokter || (komponenDiskonMap != null && listKomp.Any(k => k.Diskon > 0 && IsKomponenDokter(k)));
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, pakaiBedId, qty, hasDoctorDiskon);
        return result;
    }

    public static TindakanModel Create(RegModel reg, 
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, string pakaiBedId, int qty = 1, DateTime occurredAt = default,
        bool isDiskonDokter = false,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
        => Create(reg, layanan, nilaiTarif, listKomponenPpaView, userId, occurredAt, pakaiBedId, qty, isDiskonDokter, komponenDiskonMap);
    
    public static TindakanModel Save(string tindakanId, RegModel reg,
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView,
        AuditTrailType auditTrail, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1,
        bool isDiskonDokter = false,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1 / Qty must be at least 1");

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty, isDiskonDokter, reg.Dokter, komponenDiskonMap);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        var hasDoctorDiskon = isDiskonDokter || (komponenDiskonMap != null && listKomp.Any(k => k.Diskon > 0 && IsKomponenDokter(k)));

        var result = new TindakanModel(tindakanId, occurredAt, "",
            reg.ToReff(), layanan.ToReff(), nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, auditTrail, pakaiBedId, qty, hasDoctorDiskon);
        return result;
    }

    public static TindakanModel Save(string tindakanId, RegModel reg,
        LayananType layanan, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView,
        AuditTrailType auditTrail, string pakaiBedId, int qty = 1, DateTime occurredAt = default,
        bool isDiskonDokter = false,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
        => Save(tindakanId, reg, layanan, nilaiTarif, listKomponenPpaView, auditTrail, occurredAt, pakaiBedId, qty, isDiskonDokter, komponenDiskonMap);

    public static TindakanModel FromReg(RegModel reg, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, DateTime occurredAt = default,
        string pakaiBedId = "", int qty = 1,
        bool isDiskonDokter = false)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty tindakan minimal 1 / Qty must be at least 1");

        var newId = GenerateNewId();

        var listKomp = GenListKomponen(nilaiTarif, listKomponenPpaView, qty, isDiskonDokter, reg.Dokter);
        var audit = AuditTrailType.Create(userId, occurredAt);
        var tarif = new TarifReff(nilaiTarif.TarifId, nilaiTarif.TarifName);
        
        var result = new TindakanModel(newId, occurredAt, "",
            reg.ToReff(), reg.Layanan, nilaiTarif.Kelas, nilaiTarif.TipeTarif,
            tarif, listKomp, audit, pakaiBedId, qty, isDiskonDokter);
        return result;
    }

    public static TindakanModel FromReg(RegModel reg, NilaiTarifType nilaiTarif,
        IEnumerable<KomponenPpaView> listKomponenPpaView, 
        string userId, string pakaiBedId, int qty = 1, DateTime occurredAt = default,
        bool isDiskonDokter = false)
        => FromReg(reg, nilaiTarif, listKomponenPpaView, userId, occurredAt, pakaiBedId, qty, isDiskonDokter);

    public static TindakanModel Load(
        string tindakanId, DateTime tindakanDate, string orderTindakanId,
        RegReff reg, LayananReff layanan, KelasReff kelas,
        TipeTarifReff tipeTarif, TarifReff tarif,
        IEnumerable<TindakanKomponenBase> listKomponen, 
        AuditTrailType auditTrail,
        string pakaiBedId = "",
        int qty = 1,
        bool isDiskonDokter = false)
        => new(tindakanId, tindakanDate, orderTindakanId, reg, layanan, kelas,
            tipeTarif, tarif, listKomponen, auditTrail, pakaiBedId, qty, isDiskonDokter);

    private static IEnumerable<TindakanKomponenBase> GenListKomponen(
        NilaiTarifType nilaiTarif, IEnumerable<KomponenPpaView> listKomponenPpaView,
        int qty = 1, bool isDiskonDokter = false, PpaReff? dpjp = null,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
    {
        var result = new List<TindakanKomponenBase>();
        var listKompPpaFetched = listKomponenPpaView?.ToList() ?? [];

        string? targetKomponenId = null;
        if (isDiskonDokter && (komponenDiskonMap == null || komponenDiskonMap.Count == 0))
        {
            var doctorKomps = new List<(NilaiTarifKomponenType item, KomponenPpaView? kompPpa)>();
            foreach (var item in nilaiTarif.ListKomponen)
            {
                var kompPpa = listKompPpaFetched.FirstOrDefault(x =>
                    x.Komponen.KomponenId == item.Komponen.KomponenId || x.Komponen.ToReff() == item.Komponen);
                if (IsDoctorKomponen(item, kompPpa))
                {
                    doctorKomps.Add((item, kompPpa));
                }
            }

            if (doctorKomps.Count == 1)
            {
                targetKomponenId = doctorKomps[0].item.Komponen.KomponenId;
            }
            else if (doctorKomps.Count > 1)
            {
                var dpjpId = dpjp?.PpaId;
                var matchDpjp = !string.IsNullOrWhiteSpace(dpjpId) && dpjpId != "-"
                    ? doctorKomps.FirstOrDefault(x => x.kompPpa?.Ppa.PpaId == dpjpId)
                    : default;

                targetKomponenId = matchDpjp.item != null
                    ? matchDpjp.item.Komponen.KomponenId
                    : doctorKomps[0].item.Komponen.KomponenId;
            }
        }
        
        foreach (var item in nilaiTarif.ListKomponen)
        {
            var kompPpa = listKompPpaFetched
                .FirstOrDefault(x => x.Komponen.KomponenId == item.Komponen.KomponenId || x.Komponen.ToReff() == item.Komponen);

            decimal diskon = 0m;
            var maxKomponenSubTotal = item.Nilai * qty;

            if (komponenDiskonMap != null && komponenDiskonMap.TryGetValue(item.Komponen.KomponenId, out var customDiskon))
            {
                diskon = Math.Clamp(customDiskon, 0m, maxKomponenSubTotal);
            }
            else if (isDiskonDokter && item.Komponen.KomponenId == targetKomponenId)
            {
                diskon = maxKomponenSubTotal;
            }

            var subTotal = maxKomponenSubTotal - diskon;
            
            TindakanKomponenBase newItem = kompPpa is null 
                ? new TindakanKomponenWithoutPpaType(item.Komponen, item.NoUrut, item.Nilai, qty, subTotal, diskon) 
                : TindakanKomponenWithPpaType.Create(kompPpa.Komponen, kompPpa.Ppa, 
                    item.NoUrut, item.Nilai, qty, diskon);
            
            result.Add(newItem);
        }
        
        return result.AsEnumerable();
    }

    private static bool IsKomponenDokter(TindakanKomponenBase komp)
    {
        if (komp is TindakanKomponenWithPpaType withPpa && !string.IsNullOrWhiteSpace(withPpa.Ppa.PpaName))
        {
            if (withPpa.Ppa.PpaName.StartsWith("dr", StringComparison.OrdinalIgnoreCase) ||
                withPpa.Ppa.PpaName.Contains("dr.", StringComparison.OrdinalIgnoreCase) ||
                withPpa.Ppa.PpaName.Contains("Sp.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (komp.Komponen.KomponenName.Contains("Dokter", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsDoctorKomponen(NilaiTarifKomponenType item, KomponenPpaView? kompPpa)
    {
        if (kompPpa?.Ppa != null && kompPpa.Ppa.IsDokter())
            return true;

        if (kompPpa?.Komponen?.ListSatTugas != null && kompPpa.Komponen.ListSatTugas.Any(st =>
            st.Profesi == ProfesiType.Dokter ||
            st.SatTugasId.Contains("MD", StringComparison.OrdinalIgnoreCase) ||
            st.SatTugasId.Contains("DOKTER", StringComparison.OrdinalIgnoreCase) ||
            st.SatTugasName.Contains("Dokter", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (kompPpa?.Ppa?.ListSatTugas != null && kompPpa.Ppa.ListSatTugas.Any(st =>
            st.SatTugas.Profesi == ProfesiType.Dokter ||
            st.SatTugas.SatTugasId.Contains("MD", StringComparison.OrdinalIgnoreCase) ||
            st.SatTugas.SatTugasId.Contains("DOKTER", StringComparison.OrdinalIgnoreCase) ||
            st.SatTugas.SatTugasName.Contains("Dokter", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (item.Komponen.KomponenName.Contains("Dokter", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
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
        1,
        false
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
    public int Qty { get; init; } = 1;
    public bool IsDiskonDokter { get; init; }
    public decimal DiskonDokter => _listKomponen.Sum(t => t.Diskon);
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
    RegReff Reg, LayananReff Layanan, TarifReff Tarif, string PakaiBedId = "") : ITindakanKey;

public record TindakanJualView(string TransaksiId, DateTime TransaksiDate, string OrderTrasaksi, 
    RegReff Reg, LayananReff Layanan, string DiskripsiId, string DiskripsiName, 
    int Qty, decimal Total, string Tipe, string PakaiBedId = "");
