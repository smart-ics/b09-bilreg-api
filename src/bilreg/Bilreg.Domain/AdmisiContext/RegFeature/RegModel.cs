using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegModel : IRegKey
{
    private readonly List<RegKomponenType> _listKomponen;
    private const string BAYAR_SENDIRI = "00000";

    #region  CREATION
    public RegModel(string regId, DateOnly regDate,
        AuditInfoType regMasukAudit, AuditInfoType regKeluarAudit, 
        AuditInfoType regCancelOutAudit, AuditInfoType regVoidAudit,
        JenisRegEnum jenisReg, PasienReff pasien, TipeJaminanReff tipeJaminan, 
        PolisReff polis, KelasReff kelas, CaraMasukDkType caraMasukDk, RujukanReff rujukan, 
        PpaReff dokter, LayananReff layanan, KarcisReff karcis, 
        RegEligibilityType eligibility,
        IEnumerable<RegKomponenType> listKomponen,
        KelasDkType? kelasDk = null,
        BangsalReff? bangsal = null)
    {
        RegId = regId;
        RegDate = regDate;
        RegMasukAudit = regMasukAudit;
        RegKeluarAudit = regKeluarAudit;
        RegCancelOutAudit = regCancelOutAudit;
        RegVoidAudit = regVoidAudit;
        JenisReg = jenisReg;
        Pasien = pasien;
        TipeJaminan = tipeJaminan;
        Polis = polis;
        Kelas = kelas;
        CaraMasukDk = caraMasukDk;
        Rujukan = rujukan;
        Dokter = dokter;
        Layanan = layanan;
        Karcis = karcis;
        Eligibility = eligibility;
        KelasDk = kelasDk ?? KelasDkType.Default;
        Bangsal = bangsal ?? new BangsalReff("-", "-");
        _listKomponen = listKomponen?.ToList() ?? [];
    }

    public static RegModel Default => new RegModel("-", new DateOnly(3000, 1, 1),
        AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
        JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
        RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
        KarcisType.Default.ToReff(), 
        new RegEligibilityType("-", "-", "-"), []);
    
    public static IRegKey Key(string id) => new RegModel(id, new DateOnly(3000, 1, 1),
        AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
        JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
        RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
        KarcisType.Default.ToReff(), 
        new RegEligibilityType("-", "-", "-"), []);
    #endregion

    #region PROPERTIES
    //      entitas identity
    public string RegId { get; init; } 
    public DateOnly RegDate { get; init; }
    public AuditInfoType RegMasukAudit { get; private set; }
    public AuditInfoType RegKeluarAudit { get; private set;}
    public AuditInfoType RegCancelOutAudit { get; private set; }
    public AuditInfoType RegVoidAudit { get; private set;  }
    public bool IsAktif => RegKeluarAudit == AuditInfoType.Default
        && RegVoidAudit == AuditInfoType.Default;
    public JenisRegEnum JenisReg { get; init; }
    //      siapa yang berobat
    public PasienReff Pasien { get; init; }
    //      bagaimana bayarnya
    public TipeJaminanReff TipeJaminan { get; private set; }
    public PolisReff Polis { get; private set; }
    public KelasReff Kelas { get; private set; }
    //      dari mana asalnya
    public CaraMasukDkType CaraMasukDk { get; private set; }
    public RujukanReff Rujukan { get; private set; }
    //      ke mana (catat tujuan utama di header)
    public PpaReff Dokter { get; private set; }
    public LayananReff Layanan { get; private set; }
    public KarcisReff Karcis { get; private set; }
    //      Sync enrichment for Admission bridge; not persisted in ta_registrasi.
    public KelasDkType KelasDk { get; init; }
    public BangsalReff Bangsal { get; init; }
    //      Eligibility
    public RegEligibilityType Eligibility { get; private set; }
    //
    public IEnumerable<RegKomponenType> ListKomponen => _listKomponen;
    #endregion
    
    #region BEHAVIOUR
    public RegReff ToReff()=> new RegReff(RegId, Pasien.PasienId, Pasien.PasienName);

    public void ApplyJaminan(TipeJaminanType tipeJaminan, PolisModel polis)
    {
        if (tipeJaminan.TipeJaminanId == BAYAR_SENDIRI)
        {
            TipeJaminan = tipeJaminan.ToReff();
            Polis = PolisModel.Default.ToReff();
            return;
        }

        if (polis.TipeJaminan != tipeJaminan.ToReff())
            throw new ArgumentException("Polis tidak sesuai dengan tipe jaminan");

        if (polis.ListCover.All(x => x.Pasien.PasienId != Pasien.PasienId))
            throw new ArgumentException($"Polis '{polis.NoPolis}' tidak meng-cover pasien '{Pasien.PasienName}'");
        
        TipeJaminan = tipeJaminan.ToReff();
        Polis = polis.ToReff();
    }

    public void SpecifyCaraMasuk(CaraMasukDkType caraMasukDk, RujukanType rujukan)
    {
        if (caraMasukDk == CaraMasukDkType.DatangSendiri)
        {
            CaraMasukDk = caraMasukDk;
            Rujukan = rujukan.ToReff();
            return;
        }

        if (rujukan.CaraMasukDk != caraMasukDk)
            throw new ArgumentException($"""
                 Cara Masuk Rujukan {rujukan.RujukanName} adalah {rujukan.CaraMasukDk.CaraMasukDkName}.
                 Registrasi gagal
                 """);

        CaraMasukDk = caraMasukDk;
        Rujukan = rujukan.ToReff();
    }

    public void ChangeDataKunjungan(LayananType layanan, KarcisType karcis, PpaType dokter)
    {
        if (layanan.InstalasiDk.InstalasiDkId != "2")
            throw new ArgumentException($"Layanan {layanan.LayananId} bukan instalasi rawat jalan");
        
        if (!karcis.IsValidLayanan(layanan))
            throw new InvalidOperationException("Karcis tidak valid untuk layanan ini");

        Dokter = dokter.ToReff();
        Layanan = layanan.ToReff();
        Karcis = karcis.ToReff();
    }
    
    public void ChangeJaminan(TipeJaminanType tipeJaminan, PolisModel polis, CaraMasukDkType caraMasuk, 
        RujukanType rujukan, KarcisType karcis, LayananType layanan)
    {
        if (!karcis.IsValidLayanan(layanan))
            throw new InvalidOperationException("Karcis tidak valid untuk layanan ini");

        TipeJaminan = tipeJaminan.ToReff();
        Polis = polis.ToReff();
        CaraMasukDk = caraMasuk;
        Rujukan = rujukan.ToReff();
        Karcis = karcis.ToReff();
    }

    public void AssignVisitTo(PpaType dokter, LayananType layanan, KarcisType karcis)
    {
        if (karcis.ListLayanan.All(x => x.LayananId != layanan.LayananId))
            throw new ArgumentException($"Layanan {layanan.LayananName} tidak terdaftar di karcis {karcis.KarcisName}");
        
        Dokter = dokter.ToReff();
        Layanan = layanan.ToReff();
        Karcis = karcis.ToReff();
        
        _listKomponen.Clear();
        _listKomponen.AddRange(karcis.ListKomponen
            .Select(x => new RegKomponenType(x.KomponenTarif, dokter.ToReff(), x.Nilai, 0)));
    }

    public void AssignInpatientVisitTo(PpaType dokter, LayananType layanan, KarcisType karcis)
    {
        if (layanan.InstalasiDk.InstalasiDkId != InstalasiDkType.RawatInap.InstalasiDkId)
            throw new ArgumentException($"Layanan {layanan.LayananId} bukan instalasi rawat inap");

        if (!karcis.IsValidLayanan(layanan))
            throw new ArgumentException($"Layanan {layanan.LayananName} tidak terdaftar di karcis {karcis.KarcisName}");

        Dokter = dokter.ToReff();
        Layanan = layanan.ToReff();
        Karcis = karcis.ToReff();

        // Rawat Inap does not create ta_registrasi2 rows (Rawat Jalan / IGD only).
        _listKomponen.Clear();
    }

    public void BatalBerobat(string userId, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId wajib diisi.", nameof(userId));

        if (RegVoidAudit != AuditInfoType.Default)
            throw new InvalidOperationException($"Registrasi {RegId} sudah dibatalkan.");

        if (RegKeluarAudit != AuditInfoType.Default)
            throw new InvalidOperationException($"Registrasi {RegId} sudah keluar dan tidak dapat dibatalkan.");

        RegVoidAudit = new AuditInfoType(userId, timestamp);
    }

    // Retained for existing registration-cancellation callers.
    public void BatalBerobat(string userId) => BatalBerobat(userId, DateTime.Now);

    public void SetEligibility(string noSjp, string pesertaJaminanId, string sjpId)
    {
        var data = new RegEligibilityType(sjpId, noSjp, pesertaJaminanId);
        Eligibility = data;
    }

    #endregion
}

public record RegKomponenType(
    KomponenReff Komponen,
    PpaReff Ppa,
    decimal Nilai,
    decimal Diskon);

public record RegReff(
    string RegId,
    string PasienId,
    string PasienName);
public record RegEligibilityType(
    string SjpId,
    string SjpNo,
    string PesertaJaminanId)
{
    public static RegEligibilityType Default
        => new RegEligibilityType("-", "-", "-");
}
    
