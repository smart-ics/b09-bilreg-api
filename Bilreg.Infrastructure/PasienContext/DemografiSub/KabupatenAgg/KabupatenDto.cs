using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;

public class KabupatenDto
{
    public string KabupatenId { get; set; }
    public string KabupatenName { get; set; }
    public string PropinsiId { get; set; }
    public string PropinsiName { get; set; }

    public KabupatenModel ToModel()
    {
        var propinsi = new PropinsiModel(PropinsiId, PropinsiName);
        var response = new KabupatenModel(KabupatenId, KabupatenName, propinsi);
        return response;
    }
}