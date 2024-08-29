using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanDto
{
    public string fs_kd_kelurahan { get; set; }
    
    public string fs_nm_kelurahan { get; set; }
    
    public string fs_kd_pos { get; set; }
    
    public string fs_kd_kecamatan { get; set; }
    
    public string fs_nm_kecamatan { get; set; }
    
    public string fs_kd_kabupaten { get; set; }
    
    public string fs_nm_kabupaten { get; set; }
    
    public string fs_kd_propinsi { get; set; }
    
    public string fs_nm_propinsi { get; set; }

    public KelurahanModel ToModel()
    {
        var kelurahan = KelurahanModel.Create(fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_pos);
        var kecamatan = KecamatanModel.Create(fs_kd_kecamatan, fs_nm_kecamatan);
        var kabupaten = KabupatenModel.Create(fs_kd_kabupaten, fs_nm_kabupaten);
        var propinsi = PropinsiModel.Create(fs_kd_propinsi, fs_nm_propinsi);
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        kelurahan.Set(kecamatan);
        return kelurahan;
    }
}