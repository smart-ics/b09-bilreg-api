namespace Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

public class GrupJaminanModel: IGrupJaminanKey
{
    public GrupJaminanModel(string id, string name,
        bool isKaryawan, string keterangan)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid GrupJaminan");

        GrupJaminanId = id;
        GrupJaminanName = name;
        IsKaryawan = isKaryawan;
        Keterangan = keterangan;
    }

    public GrupJaminanModel()
    {
    }
    public static GrupJaminanModel Default =>
        new GrupJaminanModel(string.Empty, string.Empty, false, string.Empty);

    public string GrupJaminanId { get; private set; }
    public string GrupJaminanName { get; private set; }
    public bool IsKaryawan { get; private set; }
    public string Keterangan { get; private set; }

    public GrupJaminanViewType ToViewType()
        => new GrupJaminanViewType(GrupJaminanId, GrupJaminanName);
}

public record GrupJaminanViewType(
    string GrupJaminanId, 
    string GrupJaminanName);