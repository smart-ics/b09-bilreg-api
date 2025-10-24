using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisLayananDto(
    string fs_kd_peg,
    string fs_kd_layanan,
    string fs_nm_layanan,
    bool fb_utama)
{
    public static PetugasMedisLayananDto Create(PetugasMedisType ptgMed, PetugasMedisLayananType ptgMedLyn)
        => new(ptgMed.PetugasMedisId, ptgMedLyn.Layanan.LayananId,
                ptgMedLyn.Layanan.LayananName, ptgMedLyn.IsUtama);
    
    public PetugasMedisLayananType ToModel()
    {
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new PetugasMedisLayananType(lyn, fb_utama);
        return result;
    }
    
}