using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanModel: IKecamatanKey
{
    public KecamatanModel(string id, string name, KabupatenModel kabupaten)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid Kecamatan");
        Guard.IsNotNull(kabupaten);
        KecamatanId = id;
        KecamatanName = name;
        Kabupaten = kabupaten;
    }
    public static KecamatanModel Default => new KecamatanModel(string.Empty, string.Empty, KabupatenModel.Default);

    public string KecamatanId { get; private set; }
    public string KecamatanName { get; private set; }
    public KabupatenModel Kabupaten { get; private set; }
}

public interface IKecamatanKey
{
    string KecamatanId { get; }
}