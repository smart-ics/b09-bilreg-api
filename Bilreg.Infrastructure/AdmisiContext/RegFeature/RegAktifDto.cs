using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record RegAktifDto(
    string RegId, DateTime RegDate, string PasienId, string JenisRawat, 
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
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.JenisRawat,
            model.Layanan.LayananId,
            model.Dokter.PetugasMedisId,
            model.TipeJaminan.TipeJaminanId,
            model.Layanan.LayananName,
            model.Dokter.PetugasMedisName,
            model.TipeJaminan.TipeJaminanName);
    }
    
    public RegAktifModel ToModel()
    {
        var tglLahir = DateOnly.Parse(TglLahir);
        var pasienReff = new PasienReff(PasienId, PasienName,  tglLahir, Gender);
        var layananReff = new LayananReff(LayananId, LayananName);
        var dokterReff = new PpaReff(DokterId, DokterName);
        var tipeJaminanReff = new TipeJaminanReff(TipeJaminanId, TipeJaminanName);
        return new RegAktifModel(
            RegId,
            RegDate,
            pasienReff, 
            JenisRawat,
            layananReff,
            dokterReff,
            tipeJaminanReff);
    }
}