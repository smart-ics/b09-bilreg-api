using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

public class PetugasMedisLayananType
{
    public PetugasMedisLayananType(LayananReff layanan, bool isUtama)
    {
        Guard.Against.Null(layanan);
        
        Layanan = layanan;
        IsUtama = isUtama;
    }

    public LayananReff Layanan { get; init; }
    public bool IsUtama { get; init; }
}