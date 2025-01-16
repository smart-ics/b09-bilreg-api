using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.InstalasiAgg;

public class InstalasiDto
{
    public string fs_kd_instalasi { get; set; }

    public string fs_nm_instalasi { get; set; }

    public string fs_kd_instalasi_dk { get; set; }

    public string fs_nm_instalasi_dk { get; set; }

    public InstalasiModel ToModel()
    {
        var instalasi = InstalasiModel.Create(fs_kd_instalasi, fs_nm_instalasi);
        var instalasi_dk = InstalasiDkModel.Create(fs_kd_instalasi_dk, fs_nm_instalasi_dk);
        instalasi.Set(instalasi_dk);
        return instalasi;
    }
}