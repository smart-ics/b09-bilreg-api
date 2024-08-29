using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;

namespace Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanModel: IKecamatanKey
{
    public KecamatanModel(string id, string name)
    {
        KecamatanId = id;
        KecamatanName = name;
        KabupatenId = string.Empty;
        KabupatenName = string.Empty;
        PropinsiId = string.Empty;
        PropinsiName = string.Empty;
    }

    public static KecamatanModel Create(string id, string name) => new KecamatanModel(id, name);

    public void Set(KabupatenModel kabupaten)
    {
        KabupatenId = kabupaten.KabupatenId;
        KabupatenName = kabupaten.KabupatenName;
        PropinsiId = kabupaten.PropinsiId;
        PropinsiName = kabupaten.PropinsiName;
    }
    
    public string KecamatanId { get; private set; }
    public string KecamatanName { get; private set; }
    public string KabupatenId { get; private set; }
    public string KabupatenName { get; private set; }
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set; }
}

public interface IKecamatanKey
{
    string KecamatanId { get; }
}