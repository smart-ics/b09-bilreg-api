namespace Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;

public class SukuModel : ISukuKey
{
    //  CONSTRUCTOR
    private SukuModel(string id, string name)
        => (SukuId, SukuName) = (id, name);

    //  FACTORY METHODS
    public static SukuModel Create(string id, string name) 
        => new SukuModel(id, name);
    
    //  PROPERTIES
    public string SukuId { get; private set; }
    public string SukuName { get; private set; }
}

public interface ISukuKey
{
    string SukuId {get;}
}