namespace Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

public class GrupJaminanModel: IGrupJaminanKey
{
    public string GrupJaminanId { get; private set; }
    public string GrupJaminanName { get; private set; }
    public bool IsKaryawan { get; private set; }
    public string Keterangan { get; private set; }

    public GrupJaminanModel(string id, string name, string keterangan)
    {
        GrupJaminanId = id;
        GrupJaminanName = name;
        Keterangan = keterangan;
        IsKaryawan = false;
    }

    public void SetKaryawan() => IsKaryawan = true;
    public void UnSetKaryawan() => IsKaryawan = false;
    
    public static GrupJaminanModel Create(string id, string name, string keterangan) => new GrupJaminanModel(id, name, keterangan);
}

public interface IGrupJaminanKey
{
    string GrupJaminanId { get; }
}