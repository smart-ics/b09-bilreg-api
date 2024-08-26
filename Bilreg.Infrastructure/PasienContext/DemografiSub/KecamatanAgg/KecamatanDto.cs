using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanDto
{
    public string fs_kd_kecamatan { get; set; }
    
    public string fs_nm_kecamatan { get; set; }
    
    public string fs_kd_kabupaten { get; set; }
    
    public string fs_nm_kabupaten { get; set; }

    public KecamatanModel ToModel()
    {
        var kecamatan = KecamatanModel.Create(fs_kd_kecamatan, fs_nm_kecamatan);
        var kabupaten = KabupatenModel.Create(fs_kd_kabupaten, fs_nm_kabupaten);
        kecamatan.Set(kabupaten);
        return kecamatan;
    }
}