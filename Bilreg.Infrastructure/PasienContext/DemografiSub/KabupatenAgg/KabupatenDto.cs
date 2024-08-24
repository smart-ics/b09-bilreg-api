using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;

public class KabupatenDto
{
    public string fs_kd_kabupaten { get; set; }
    public string fs_nm_kabupaten { get; set; }
    public string fs_kd_propinsi { get; set; }
    public string fs_nm_propinsi { get; set; }

    public KabupatenModel ToModel()
    {
        var response = KabupatenModel.Create(fs_kd_kabupaten, fs_nm_kabupaten);
        var propinsi = PropinsiModel.Create(fs_kd_propinsi, fs_nm_propinsi);
        response.Set(propinsi);
        return response;
    }
}