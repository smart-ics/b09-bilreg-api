using Bilreg.Domain.BedUsageContext.RoomRateFeature;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

public record TipeKamarDto(string fs_kd_kamar_tipe, string fs_nm_kamar_tipe, 
    bool fb_gabung, bool fb_aktif, bool fb_default_tipe)
{
    public static TipeKamarDto FromModel(TipeKamarType model)
    {
        var result = new TipeKamarDto(model.TipeKamarId, model.TipeKamarName, 
            model.IsGabung, model.IsAktif, model.IsDefault);
        return result;
    }

    public TipeKamarType ToModel()
    {
        var result = new TipeKamarType(fs_kd_kamar_tipe, fs_nm_kamar_tipe, 
            fb_default_tipe, fb_gabung, fb_aktif);
        return result;
    }
}