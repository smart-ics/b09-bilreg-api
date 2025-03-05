namespace Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;

public class SukuModel : ISukuKey
{
    public SukuModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException(("Invalid suku."));
        
        SukuId = id;
        SukuName = name;
    }
    public static SukuModel Default => new SukuModel(string.Empty, string.Empty);
    public SukuModel()
    {
    }

    public string SukuId { get; private set; }
    public string SukuName { get; private set; }
}