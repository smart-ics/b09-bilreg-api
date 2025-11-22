using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaLayananDto(
    string fs_kd_peg,
    string fs_kd_layanan,
    decimal fb_utama,
    string fs_nm_layanan)
{
    public static PpaLayananDto Create(PpaType ptgMed, PpaLayananType ptgMedLyn)
        => new(ptgMed.PpaId, ptgMedLyn.Layanan.LayananId,
                ptgMedLyn.IsUtama ? 1 : 0, ptgMedLyn.Layanan.LayananName);
    
    public PpaLayananType ToModel()
    {
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new PpaLayananType(lyn, fb_utama == 1);
        return result;
    }
    
}

