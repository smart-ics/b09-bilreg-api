using Bilreg.Domain.AdmisiContext.JaminanSub;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.TipeJaminanAgg;

public record TipeJaminanDto(
    string fs_kd_tipe_jaminan, 
    string fs_nm_tipe_jaminan,
    bool fb_aktif, 
    string fs_kd_jaminan, 
    string fs_nm_jaminan,
    string fs_kd_cara_bayar_dk,
    string fs_nm_cara_bayar_dk)
{
    public static TipeJaminanDto FromModel(TipeJaminanType model)
    {
        return new TipeJaminanDto(
            model.TipeJaminanId,
            model.TipeJaminanName,
            model.IsAktif,
            model.Jaminan.JaminanId,
            model.Jaminan.JaminanName,
            model.CaraBayarDk.CaraBayarDkId,
            model.CaraBayarDk.CaraBayarDkName);
    }

    public TipeJaminanType ToModel()
    {
        var jaminan = new JaminanReff(fs_kd_jaminan, fs_nm_jaminan);
        var caraBayarDk = new CaraBayarDkType(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
        var tipeJaminan = new TipeJaminanType(
            fs_kd_tipe_jaminan, fs_nm_tipe_jaminan, fb_aktif,
            jaminan, caraBayarDk);
        return tipeJaminan;
    }
}