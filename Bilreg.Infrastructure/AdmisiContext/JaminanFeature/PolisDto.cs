using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
public record PolisDto(
    string fs_kd_polis,
    string fs_kd_tipe_jaminan,
    string fs_kd_kelas_ri,
    string fs_no_polis,
    string fs_atas_nama,
    string fd_expired,
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
            model.Kelas.KelasId,
            model.NoPolis,
            model.AtasNama,
            model.ExpiredDate.ToString("yyyy-MM-dd"),
            model.IsCoverRajal,
            model.TipeJaminan.TipeJaminanName,
            model.Kelas.KelasName);
    }
}

