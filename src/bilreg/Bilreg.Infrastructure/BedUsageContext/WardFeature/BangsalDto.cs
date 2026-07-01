using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public record BangsalDto(string fs_kd_bangsal, string fs_nm_bangsal, 
    string fs_kd_layanan, string fs_kd_roomcat, string fs_nm_layanan, string fs_nm_roomcat)
{
    public static BangsalDto FromModel(BangsalType model)
    {
        var result = new BangsalDto(model.BangsalId, model.BangsalName, 
            model.Layanan.LayananId, model.RoomCat.RoomCatId, 
            model.Layanan.LayananName, model.RoomCat.RoomCatName);
        return result;
    }

    public BangsalType ToModel()
    {
        var roomCat = new RoomCatType(fs_kd_roomcat, fs_nm_roomcat);
        var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new BangsalType(fs_kd_bangsal, fs_nm_bangsal, roomCat, layanan);
        return result;
    }
}