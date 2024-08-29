namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisLayananModel(string petugasId, string layananId, string layananName)
{
    public string PetugasId { get; private set; } = petugasId;
    public string LayananId { get; private set; } = layananId;
    public string LayananName { get; private set; } = layananName;
}