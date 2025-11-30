using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub;

// resharper disable InconsistentNaming
public record RujukanDto(
    string fs_kd_rujukan,
    string fs_nm_rujukan,
    string fs_alm_rujukan,
    string fs_alm2_rujukan,
    string fs_kota_rujukan,
    string fs_tlp_rujukan,
    string fs_kd_rujukan_tipe,
    string fs_kd_kelas_rs,
    string fs_kd_cara_masuk_dk,
    bool fb_aktif,
    //
    string fs_nm_rujukan_tipe,
    string fs_nm_kelas_rs,
    string fs_nm_cara_masuk_dk)
{
    public static RujukanDto FromModel(RujukanType model)
    {
        var result = new RujukanDto(
            model.RujukanId, model.RujukanName,
            model.Alamat.Alamat[0],
            model.Alamat.Alamat[1],
            model.Alamat.Kota,
            model.NoTelp.ContactDetail,
            model.TipeRujukan.TipeRujukanId,
            model.KelasRujukan.KelasRujukanId,
            model.CaraMasukDk.CaraMasukDkId,
            model.IsAktif,
            //
            model.TipeRujukan.TipeRujukanName,
            model.KelasRujukan.KelasRujukanName,
            model.CaraMasukDk.CaraMasukDkId);
        return result;
    }

    public RujukanType ToModel()
    {
        var alamat = new AlamatType([fs_alm_rujukan, 
            fs_alm2_rujukan], fs_kota_rujukan, "-");
        var noTelp = new ContactType(JenisContactEnum.Phone, fs_tlp_rujukan);
        var result = 
            new RujukanType(fs_kd_rujukan,
                fs_nm_rujukan, fb_aktif, alamat, noTelp,
                new TipeRujukanType(fs_kd_rujukan_tipe, fs_nm_rujukan_tipe),
                new KelasRujukanReff(fs_kd_kelas_rs, fs_nm_kelas_rs),
                new CaraMasukDkType(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk));
        return result;
    }
}