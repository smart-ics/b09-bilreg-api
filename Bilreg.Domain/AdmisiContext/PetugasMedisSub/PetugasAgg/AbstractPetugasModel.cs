using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public abstract class AbstractPetugasModel
{
    protected AbstractPetugasModel(string id, string name)
    {
        PetugasId = id;
        PetugasName = name;
    }
    public string PetugasId { get; private set; }
    public string PetugasName { get; private set; }
    public string NamaSingkat { get; private set; }
    public string ReffPegId { get; private set; }
    public string JenisPetugas { get; private set; }

    public static AbstractPetugasModel Create(string id, string name, string jenisPetugas)
    {
        var result = jenisPetugas switch
        {
            "Dokter" => new DokterModel(id, name),
            _ => throw new ArgumentOutOfRangeException(nameof(jenisPetugas), jenisPetugas, null)
        };
        return result;
    }
}

