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
        var propinsi = new PropinsiModel(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenModel(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanModel(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten);

        return kecamatan;
    }
}