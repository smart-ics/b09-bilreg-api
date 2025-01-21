using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;

namespace Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanModel: IKecamatanKey
{
    public KecamatanModel(string id, string name, KabupatenModel kabupaten)
    {
        KecamatanId = id;
        KecamatanName = name;
        Kabupaten = kabupaten;
    }

    public string KecamatanId { get; private set; }
    public string KecamatanName { get; private set; }
    public KabupatenModel Kabupaten { get; private set; }
}

public interface IKecamatanKey
{
    string KecamatanId { get; }
}