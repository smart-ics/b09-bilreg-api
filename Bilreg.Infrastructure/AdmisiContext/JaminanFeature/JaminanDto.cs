using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public record JaminanDto(
    string fs_kd_jaminan, 
    string fs_nm_jaminan ,
    string fs_alm1_jaminan ,
    string fs_alm2_jaminan ,
    string fs_kota_jaminan ,
    bool fb_aktif ,
    string fs_benefit_mou,
    string fs_kd_cara_bayar_dk ,
    string fs_kd_grup_jaminan,
    string fs_kd_pos ,
    string fs_nm_cara_bayar_dk ,
    string fs_nm_grup_jaminan )
    
    
{
    public JaminanType ToModel()
    {
        var caraBayarDk = new CaraBayarDkType(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
        var grupJaminan = new GroupJaminanReff(fs_kd_grup_jaminan, fs_nm_grup_jaminan);
        var alamat = new AlamatType([fs_alm1_jaminan, fs_alm2_jaminan], fs_kota_jaminan, fs_kd_pos);
        var jaminan = new JaminanType(fs_kd_jaminan, fs_nm_jaminan, fb_aktif,
            alamat, caraBayarDk, grupJaminan);

        return jaminan;
    }
}