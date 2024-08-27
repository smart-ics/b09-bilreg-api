namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
public class SatuanTugasModel : ISatuanTugasKey
{
    // CONSTRUCTOR
    private SatuanTugasModel(string id, string name, bool isMedis)
    {
        SatuanTugasId = id;
        SatuanTugasName = name;
        IsMedis = isMedis;
    }

    // FACTORY METHOD
    public static SatuanTugasModel Create(string id, string name, bool isMedis)
    {
        return new SatuanTugasModel(id, name, isMedis);
    }

    // PROPERTIES
    public string SatuanTugasId { get; private set; }
    public string SatuanTugasName { get; private set; }
    public bool IsMedis { get; private set; }
}

    public interface ISatuanTugasKey
    {
        string SatuanTugasId { get; }
    }

