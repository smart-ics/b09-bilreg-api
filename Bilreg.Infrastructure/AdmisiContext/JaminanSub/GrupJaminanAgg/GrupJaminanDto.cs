using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.GrupJaminanAgg;

public class GrupJaminanDto
{
    public string fs_kd_grup_jaminan { get; set; }
    public string fs_nm_grup_jaminan { get; set; }
    public bool fb_karyawan { get; set; }
    public string fs_keterangan { get; set; }

    public GrupJaminanModel ToModel()
    {
        var grupJaminan = GrupJaminanModel.Create(fs_kd_grup_jaminan, fs_nm_grup_jaminan, fs_keterangan);

        if (fb_karyawan)
            grupJaminan.SetKaryawan();
        else
            grupJaminan.UnSetKaryawan();
        return grupJaminan;
    }
}