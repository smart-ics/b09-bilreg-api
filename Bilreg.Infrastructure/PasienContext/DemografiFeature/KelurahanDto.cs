using Bilreg.Domain.PasienContext.DemografiFeature;
// ReSharper disable All

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public record KelurahanDto(string fs_kd_kelurahan, string fs_nm_kelurahan,
    string fs_kd_kecamatan, string fs_nm_kecamatan, 
    string fs_kd_kabupaten, string fs_nm_kabupaten, 
    string fs_kd_propinsi, string fs_nm_propinsi)
{
    public KelurahanType ToModel()
    {
        var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanType(fs_kd_kecamatan, fs_nm_kecamatan,
            kabupaten.ToReff(), propinsi);
        var kelurahan = new KelurahanType(fs_kd_kelurahan, fs_nm_kelurahan,
            kecamatan.ToReff(), kabupaten.ToReff(), propinsi);

        return kelurahan;
    }
}