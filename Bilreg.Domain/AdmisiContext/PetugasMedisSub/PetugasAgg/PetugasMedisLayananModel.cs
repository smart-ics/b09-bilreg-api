using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Ardalis.GuardClauses;
namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisLayananModel
{
    public PetugasMedisLayananModel(LayananReff layanan, bool isUtama)
    {
        Guard.Against.Null(layanan);
        
        Layanan = layanan;
        IsUtama = isUtama;
    }

    public LayananReff Layanan { get; init; }
    public bool IsUtama { get; init; }
}