using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisLayananDto(
    string fs_kd_peg,
    string fs_kd_layanan,
    decimal fb_utama,
    string fs_nm_layanan)
{
    public static PetugasMedisLayananDto Create(PetugasMedisType ptgMed, PetugasMedisLayananType ptgMedLyn)
        => new(ptgMed.PetugasMedisId, ptgMedLyn.Layanan.LayananId,
                ptgMedLyn.IsUtama ? 1 : 0, ptgMedLyn.Layanan.LayananName);
    
    public PetugasMedisLayananType ToModel()
    {
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new PetugasMedisLayananType(lyn, fb_utama == 1);
        return result;
    }
    
}