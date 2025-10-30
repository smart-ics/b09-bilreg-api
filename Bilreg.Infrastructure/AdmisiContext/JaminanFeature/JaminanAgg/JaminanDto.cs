using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.PasienContext.PasienFeature;

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
    public string fs_kd_pos { get; set; }

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