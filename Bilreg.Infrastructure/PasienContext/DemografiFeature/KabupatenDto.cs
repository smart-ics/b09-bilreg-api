using Bilreg.Domain.PasienContext.DemografiFeature;
// ReSharper disable All

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;

public record KabupatenDto(string fs_kd_kabupaten,
    string fs_nm_kabupaten,
    string fs_kd_propinsi,
    string fs_nm_propinsi)
{
    public KabupatenType ToModel()
    {
        var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
        var response = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        return response;
    }
}