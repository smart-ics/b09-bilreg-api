using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;

public class KabupatenModel : IKabupatenKey
{
    public KabupatenModel(string id, string name, PropinsiModel propinsi)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid Kabupaten");
        
        Guard.IsNotNull(propinsi);
        
        KabupatenId = id;
        KabupatenName = name;
        Propinsi = propinsi;
    }
    public static KabupatenModel Default => new(string.Empty, string.Empty, PropinsiModel.Default);

    public string KabupatenId { get; private set; }
    public string KabupatenName { get; private set; }
    public PropinsiModel Propinsi { get; private set; }
}

