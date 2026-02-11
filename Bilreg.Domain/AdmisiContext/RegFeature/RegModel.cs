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
        AuditInfoType regMasukAudit, AuditInfoType regKeluarAudit, AuditInfoType regCancelOutAudit,
        JenisRegEnum jenisReg, PasienReff pasien, TipeJaminanReff tipeJaminan, 
        PolisReff polis, KelasReff kelas, CaraMasukDkType caraMasukDk, RujukanReff rujukan, 
        PpaReff dokter, LayananReff layanan, KarcisReff karcis, 
        IEnumerable<RegKomponenType> listKomponen)
    {
        RegId = regId;
        RegDate = regDate;
        RegMasukAudit = regMasukAudit;
        RegKeluarAudit = regKeluarAudit;
        RegCancelOutAudit = regCancelOutAudit;
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
        _listKomponen = listKomponen.ToList();
    }

    public static RegModel Default => new RegModel("-", new DateOnly(3000, 1, 1),
        AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
        JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
        RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
        KarcisType.Default.ToReff(), []);
    
    public static IRegKey Key(string id) => new RegModel(id, new DateOnly(3000, 1, 1),
        AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
        JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
        RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
        KarcisType.Default.ToReff(), []);
    #endregion

    #region PROPERTIES
    //      entitas identity
    public string RegId { get; init; } 
    public DateOnly RegDate { get; init; }
    public AuditInfoType RegMasukAudit { get; init; }
    public AuditInfoType RegKeluarAudit { get; private set;}
    public AuditInfoType RegCancelOutAudit { get; private set; }
    public bool IsAktif => RegKeluarAudit == AuditInfoType.Default;
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

    public void ChangeLayanan(LayananType layanan, KarcisType karcis)
    {
        //  TODO: Layanan harus sesuai tipe registrasi
        
        //  TODO: Karcis harus sesuai layanan dan hanya untuk registrasi r.jalan
        if (!karcis.IsValidLayanan(layanan))
            throw new InvalidOperationException("Karcis tidak valid untuk layanan ini");
        
        Layanan = layanan.ToReff();
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
    
    