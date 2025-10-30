// resharper disable inconsistentnaming

using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.BillContext.BedUsageFeature;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public record PolisDto(
    string fs_kd_polis,
    string fs_kd_tipe_jaminan,
    string fs_no_polis,
    string fs_atas_nama,
    string fd_expired,
    string fs_kd_kelas_ri,
    bool fb_cover_rj,
    //
    string fs_nm_tipe_jaminan,
    string fs_nm_kelas
)
{
    public static PolisDto FromModel(PolisModel model)
    {
        return new PolisDto(
            model.PolisId,
            model.TipeJaminan.TipeJaminanId,
            model.NoPolis,
            model.AtasNama,
            model.ExpiredDate.ToString("yyyy-MM-dd"),
            model.Kelas.KelasId,
            model.IsCoverRajal,
            model.TipeJaminan.TipeJaminanName,
            model.Kelas.KelasName,
            );
    }

    public PolisModel ToModel()
    {
        var result = new PolisModel(
            fs_kd_polis,
            new TipeJaminanReff(fs_kd_tipe_jaminan, fs_nm_tipe_jaminan),
            fs_no_polis,
            fs_atas_nama,
            fd_expired,
            new KelasReff(fs_kd_kelas_ri, fs_nm_kelas),
            fb_cover_rj);
    }
}