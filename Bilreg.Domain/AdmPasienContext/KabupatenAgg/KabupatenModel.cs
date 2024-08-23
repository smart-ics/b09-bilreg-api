using Bilreg.Domain.AdmPasienContext.PropinsiAgg;

namespace Bilreg.Domain.AdmPasienContext.KabupatenAgg;

public class KabupatenModel : IKabupatenKey
{
    private KabupatenModel(string id, string name)
    {
        KabupatenId = id;
        KabupatenName = name;
        PropinsiId = string.Empty;
        PropinsiName = string.Empty;
    }

    public static KabupatenModel Create(string id, string name)
    {
        return new KabupatenModel(id, name);
    }

    public void Set(PropinsiModel propinsi)
    {
        PropinsiId = propinsi.PropinsiId;
        PropinsiName = propinsi.PropinsiName;
    }

    public string KabupatenId { get; private set; }
    public string KabupatenName { get; private set; }
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set;}
}

public interface IKabupatenKey
{
    string KabupatenId { get; }
}
