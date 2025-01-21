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
        var propinsi = new PropinsiModel(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenModel(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanModel(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten);
        var kelurahan = new KelurahanModel(fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_pos, kecamatan);
        return kelurahan;
    }
}