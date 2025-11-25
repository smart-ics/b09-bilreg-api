using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

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

public record PetugasMedisLayananView(
    string fs_kd_peg,
    string fs_kd_layanan,
    decimal fb_utama,
    string fs_nm_layanan,
    string fs_nm_peg,
    string GroupSpesialisId,
    string GroupSpesialisName
    );