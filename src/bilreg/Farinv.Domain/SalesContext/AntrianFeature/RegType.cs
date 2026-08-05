using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.SalesContext.AntrianFeature;

public record RegType : IRegKey
{
    #region  CREATION
    public RegType(string regId, DateOnly regDate, bool isAktif,
        AuditInfoType regMasukAudit, AuditInfoType regKeluarAudit, 
        int jenisReg, string jenisRegDesc, PasienReff pasien, string umur, 
        TipeJaminanReff tipeJaminan, PolisReff polis, KelasReff kelas,
        PpaReff dokter, LayananReff layanan, TipeBrgReff tipeBrg)
    {
        RegId = regId;
        RegDate = regDate;
        IsAktif = isAktif;
        RegMasukAudit = regMasukAudit;
        RegKeluarAudit = regKeluarAudit;
        JenisReg = jenisReg;
        JenisRegDesc = jenisRegDesc;    
        Pasien = pasien;
        Umur = umur;
        TipeJaminan = tipeJaminan;
        Polis = polis;
        Kelas = kelas;
        Dokter = dokter;
        Layanan = layanan;
        TipeBarang = tipeBrg;
    }

    public static RegType Default => new("-", new DateOnly(3000, 1, 1), false,
        AuditInfoType.Default, AuditInfoType.Default, 0, "-",
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"), "-", 
        new TipeJaminanReff("-", "-"), new PolisReff("-", "-", "-"),
        new KelasReff("-", "-"), new PpaReff("-", "-"),
        new LayananReff("-", "-"), new TipeBrgReff("-", "-"));
    #endregion

    #region PROPERTIES
    public string RegId { get; init; }
    public DateOnly RegDate { get; init; }
    public AuditInfoType RegMasukAudit { get; init; }
    public AuditInfoType RegKeluarAudit { get; init; }
    public bool IsAktif { get; init; }
    public int JenisReg { get; init; }
    public string JenisRegDesc { get; init; }

    public PasienReff Pasien { get; init; }
    public string Umur { get; init; }

    public TipeJaminanReff TipeJaminan { get; init; }
    public PolisReff Polis { get; init; }
    public KelasReff Kelas { get; init; }

    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    
    public TipeBrgReff TipeBarang { get; init; }
    #endregion

    #region BEHAVIOUR
    public RegReff ToReff() => new(RegId, Pasien.PasienId, Pasien.PasienName);
    #endregion
}

public interface IRegKey
{
    string RegId { get; }
}

public record RegReff(string RegId, string PasienId, string PasienName);

public record PasienReff(string PasienId, string PasienName, DateOnly TglLahir, string Gender);

public record TipeJaminanReff(string TipeJaminanId, string TipeJaminanName);

public record PolisReff(string PolisId, string NoPolis, string AtasName);

public record KelasReff(string KelasId, string KelasName);

public record PpaReff(string PpaId, string PpaName);