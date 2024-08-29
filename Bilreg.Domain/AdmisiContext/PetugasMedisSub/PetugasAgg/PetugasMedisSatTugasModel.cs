namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisSatTugasModel(string petugasId, string satTugasId, string satTugasName)
{
    //  PROPERTIES
    public string PetugasId { get; private set; } = petugasId;
    public string SatTugasId { get; private set; } = satTugasId;
    public string SatTugasName { get; private set; } = satTugasName;
    public bool IsUtama { get; private set; }

    //  BEHAVIOUR
    public void SetUtama() => IsUtama = true;
    public void UnsetUtama() => IsUtama = false;
}