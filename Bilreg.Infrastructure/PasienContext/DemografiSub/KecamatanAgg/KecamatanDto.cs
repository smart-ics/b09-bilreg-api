using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanDto
{
    public string fs_kd_kecamatan { get; set; }
    
    public string fs_nm_kecamatan { get; set; }
    
    public string fs_kd_kabupaten { get; set; }
    
    public string fs_nm_kabupaten { get; set; }
    
    public string fs_kd_propinsi { get; set; }
    
    public string fs_nm_propinsi { get; set; }

    public KecamatanModel ToModel()
    {
        var kecamatan = KecamatanModel.Create(fs_kd_kecamatan, fs_nm_kecamatan);
        var kabupaten = KabupatenModel.Create(fs_kd_kabupaten, fs_nm_kabupaten);
        var propinsi = PropinsiModel.Create(fs_kd_propinsi, fs_nm_propinsi);
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        return kecamatan;
    }
}