using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaLayananDto(
    string fs_kd_peg,
    string fs_nm_peg,
    string fs_kd_layanan,
    decimal fb_utama,
    string fs_nm_layanan)
{
    public static PpaLayananDto Create(PpaType ppa, PpaLayananType ppaLyn)
        => new(ppa.PpaId, ppa.PpaName, ppaLyn.Layanan.LayananId,
            ppaLyn.IsUtama ? 1 : 0, ppaLyn.Layanan.LayananName);
    
    public PpaLayananType ToModel()
    {
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new PpaLayananType(lyn, fb_utama == 1);
        return result;
    }

    public PpaLayananView ToView()
    {
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new PpaLayananView(fs_kd_peg, fs_nm_peg, lyn);
        return result;
    }
    
}

