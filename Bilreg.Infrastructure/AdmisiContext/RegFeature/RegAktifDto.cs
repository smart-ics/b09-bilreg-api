using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record RegAktifDto(
    string RegId, DateTime RegDate, string PasienId,string JenisReg, 
    string LayananId, string DokterId, string TipeJaminanId, 
    string PasienName, string TglLahir, string Gender,
    string LayananName, string DokterName, string TipeJaminanName)
{
    public static RegAktifDto Create(RegAktifModel model)
    {
        return new RegAktifDto(
            model.RegId,
            model.RegDate,
            model.Pasien.PasienId,
            ((int)model.JenisReg).ToString(),
            model.Layanan.LayananId,
            model.Dokter.PpaId,
            model.TipeJaminan.TipeJaminanId,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.Layanan.LayananName,
            model.Dokter.PpaName,
            model.TipeJaminan.TipeJaminanName);
    }

    public RegAktifModel ToModel()
    {
        var tglLahir = DateOnly.Parse(TglLahir);
        var pasienReff = new PasienReff(PasienId, PasienName, tglLahir, Gender);
        var layananReff = new LayananReff(LayananId, LayananName);
        var dokterReff = new PpaReff(DokterId, DokterName);
        var tipeJaminanReff = new TipeJaminanReff(TipeJaminanId, TipeJaminanName);

        var jnsReg = JenisReg switch
        {
            "0" => JenisRegEnum.RegJalan,
            "1" => JenisRegEnum.RegInap,
            "2" => JenisRegEnum.External,
            "3" => JenisRegEnum.Darurat,
            "4" => JenisRegEnum.Meninggal,
            "5" => JenisRegEnum.ExternalInap,
            _ => JenisRegEnum.Unknown
        };
        return new RegAktifModel(
            RegId,
            RegDate,
            pasienReff,
            jnsReg,
            layananReff,
            dokterReff,
            tipeJaminanReff);
    }
}