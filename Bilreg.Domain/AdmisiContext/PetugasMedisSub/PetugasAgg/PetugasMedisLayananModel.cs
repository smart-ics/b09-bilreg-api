namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisLayananModel(string petugasId, string layananId, string layananName) : IPetugasMedisKey
{
    public string PetugasMedisId { get; protected set; } = petugasId;
    public string LayananId { get; protected set; } = layananId;
    public string LayananName { get; protected set; } = layananName;
    public bool IsUtama { get; protected set; }

    
    public void SetUtama() => IsUtama = true;
    public void UnsetUtama() => IsUtama = false;
    public void SetId(string id) => PetugasMedisId = id;
}