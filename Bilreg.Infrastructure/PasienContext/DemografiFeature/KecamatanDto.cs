using Bilreg.Domain.PasienContext.DemografiFeature;
// ReSharper disable All

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanDto(string fs_kd_kecamatan, string fs_nm_kecamatan, 
    string fs_kd_kabupaten, string fs_nm_kabupaten, 
    string fs_kd_propinsi, string fs_nm_propinsi)
{
    public KecamatanType ToModel()
    {
        var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanType(fs_kd_kecamatan, fs_nm_kecamatan,
            kabupaten.ToReff(), propinsi);

        return kecamatan;
    }
}