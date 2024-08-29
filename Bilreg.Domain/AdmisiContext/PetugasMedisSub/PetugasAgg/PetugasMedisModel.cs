using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisModel
{
    private  PetugasMedisModel(string id, string name)
    {
        PetugasId = id;
        PetugasName = name;
    }
    public string PetugasId { get; private set; }
    public string PetugasName { get; private set; }
    public string NamaSingkat { get; private set; }
    public string SmfId { get; private set; }
    public string SmfName { get; private set; }

    public static PetugasMedisModel Create(string id, string name)
        => new PetugasMedisModel(id, name);
    public void Set(SmfModel smf)
        => (SmfId, SmfName) = (smf.SmfId, smf.SmfName)      ;
}

public class PetugasMedisSatTugasModel
{
    public string PetugasId { get; private set; }
    public string SatTugasId { get; private set; }
    public string SatTugasName { get; private set; }

    private PetugasMedisSatTugasModel(string petugasId)
    {
        
    }
}

