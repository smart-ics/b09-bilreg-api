using Bilreg.Domain.BedUsageContext.WardFeature;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public record KamarDto(string fs_kd_kamar, string fs_nm_kamar, 
    string fs_kd_bangsal, string fs_kd_kelas, 
    string fs_nm_bangsal, string fs_nm_kelas)
{
    public static KamarDto FromModel(KamarType model)
    {
        var result = new KamarDto(model.KamarId, model.KamarName, 
            model.Bangsal.BangsalId, model.Bangsal.BangsalName, 
            model.Kelas.KelasId, model.Kelas.KelasName);
        return result;
    }

    public KamarType ToModel()
    {
        var bangsal = new BangsalReff(fs_kd_bangsal, fs_nm_bangsal);
        var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
        var result = new KamarType(fs_kd_kamar, fs_nm_kamar, bangsal, kelas);
        return result;
    }
}