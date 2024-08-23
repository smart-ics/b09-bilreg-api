using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using Bilreg.Domain.AdmPasienContext.PropinsiAgg;

namespace Bilreg.Infrastructure.AdmPasienContext.KabupatenAgg;

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