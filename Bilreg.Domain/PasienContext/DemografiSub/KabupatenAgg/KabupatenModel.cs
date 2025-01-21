using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;

public class KabupatenModel : IKabupatenKey
{
    public KabupatenModel(string id, string name, PropinsiModel propinsi)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNullOrEmpty(name);
        Guard.IsNotNull(propinsi);
        
        KabupatenId = id;
        KabupatenName = name;
        Propinsi = propinsi;
    }

    public string KabupatenId { get; private set; }
    public string KabupatenName { get; private set; }
    public PropinsiModel Propinsi { get; private set; }
}

