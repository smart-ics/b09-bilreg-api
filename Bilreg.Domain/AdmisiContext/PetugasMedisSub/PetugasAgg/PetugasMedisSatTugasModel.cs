namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisSatTugasModel(string petugasId, string satTugasId, string satTugasName) : IPetugasMedisKey
{
    //  PROPERTIES
    public string PetugasMedisId { get; protected set; } = petugasId;
    public string SatTugasId { get; protected set; } = satTugasId;
    public string SatTugasName { get; protected  set; } = satTugasName;
    public bool IsUtama { get; protected set; }

    //  BEHAVIOUR
    public void SetUtama() => IsUtama = true;
    public void UnsetUtama() => IsUtama = false;
    public void SetId(string id) => PetugasMedisId = id;
    
}