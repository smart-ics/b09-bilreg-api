using Bilreg.Domain.BedUsageContext.WardFeature;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public record BedDto(string fs_kd_bed, string fs_nm_bed, 
    string fs_kd_kamar, bool fb_aktif, string fs_nm_kamar, string fs_kd_bangsal, string fs_nm_bangsal)
{
    public static BedDto FromModel(BedType model)
    {
        var result = new BedDto(model.BedId, model.BedName, 
            model.Kamar.KamarId, model.IsAktif, model.Kamar.KamarName, 
            model.Bangsal.BangsalId, model.Bangsal.BangsalName);
        return result;
    }

    public BedType ToModel()
    {
        var kamar = new KamarReff(fs_kd_kamar, fs_nm_kamar);
        var bangsal = new BangsalReff(fs_kd_bangsal, fs_nm_bangsal);
        var result = new BedType(fs_kd_bed, fs_nm_bed, kamar, bangsal, fb_aktif);
        return result;
    }
}