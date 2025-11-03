using Bilreg.Domain.AdmisiContext.LayananFeature;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record KarcisLayananDto(
    string fs_kd_karcis, 
    string fs_kd_layanan, 
    string fs_nm_layanan)
{
    public static KarcisLayananDto FromModel(string karcisId, LayananReff layanan)
        => new KarcisLayananDto(karcisId, layanan.LayananId, layanan.LayananName);
    
    public LayananReff ToModel()
    {
        return new LayananReff(fs_kd_layanan, fs_nm_layanan);
    }
};