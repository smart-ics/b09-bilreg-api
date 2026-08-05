using Farinv.Domain.InventoryContext.StokFeature;

//  resharper disable InconsistentNaming
namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public record LayananDto(string fs_kd_layanan, string fs_nm_layanan, string fs_kd_jenis_lokasi, string fs_nm_jenis_lokasi)
{
    public static LayananDto FromModel(LayananType model)
    {
        var result = new LayananDto(
            model.LayananId, 
            model.LayananName, 
            model.JenisLayanan.JenisLokasiId, 
            model.JenisLayanan.JenisLokasiName);
        return result;
    }

    public LayananType ToModel()
    {
        var jenisLayanan = new JenisLokasiType(fs_kd_jenis_lokasi, fs_nm_jenis_lokasi);
        var result = new LayananType(fs_kd_layanan, fs_nm_layanan, jenisLayanan);
        return result;
    }
}