using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;

public class KabupatenDto
{
    public string KabupatenId { get; set; }
    public string KabupatenName { get; set; }
    public string PropinsiId { get; set; }
    public string PropinsiName { get; set; }

    public KabupatenType ToModel()
    {
        var propinsi = new PropinsiType(PropinsiId, PropinsiName);
        var response = new KabupatenType(KabupatenId, KabupatenName, propinsi);
        return response;
    }
}