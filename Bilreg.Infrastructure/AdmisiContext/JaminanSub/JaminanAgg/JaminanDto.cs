using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanDto
{
    public string fs_kd_jaminan { get; set; }
    public string fs_nm_jaminan { get; set; }
    public string fs_alm1_jaminan { get; set; }
    public string fs_alm2_jaminan { get; set; }
    public string fs_kota_jaminan { get; set; }
    public bool fb_aktif { get; set; }
    public string fs_kd_cara_bayar_dk { get; set; }
    public string fs_nm_cara_bayar_dk { get; set; }
    public string fs_kd_grup_jaminan { get; set; }
    public string fs_nm_grup_jaminan { get; set; }
    public string fs_benefit_mou { get; set; }

    public JaminanModel ToModel()
    {
        var jaminan = JaminanModel.Create(fs_kd_jaminan, fs_nm_jaminan);
        jaminan.SetAlamat(fs_alm1_jaminan, fs_alm2_jaminan, fs_kota_jaminan);
        jaminan.SetBenefitMou(fs_benefit_mou);
        
        var caraBayarDk = CaraBayarDkModel.Create(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
        var grupJaminan = GrupJaminanModel.Create(fs_kd_grup_jaminan, fs_nm_grup_jaminan, string.Empty);
        jaminan.SetCaraBayar(caraBayarDk);
        jaminan.SetGrupJaminan(grupJaminan);
        
        if (fb_aktif)
            jaminan.SetAktif();
        else 
            jaminan.UnSetAktif();

        return jaminan;
    }
}