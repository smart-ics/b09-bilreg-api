using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

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

    public PolisModel ToModel(IEnumerable<PolisCoverModel> listCover)
    {
        var result = new PolisModel(
            fs_kd_polis, fs_no_polis, fs_atas_nama,
            new TipeJaminanReff(fs_kd_tipe_jaminan, fs_nm_tipe_jaminan),
            new KelasReff(fs_kd_kelas_ri, fs_nm_kelas),
            DateOnly.ParseExact(fd_expired, "yyyy-MM-dd", null),
            fb_cover_rj, listCover?.ToList() ?? []);
        return result;
    }
}

public record PolisViewDto(
    string fs_kd_polis,
    string fs_no_polis,
    string fs_atas_nama,
    string fs_kd_tipe_jaminan,
    string fd_tgl_expired,
    //
    string fs_mr,
    string fs_nm_pasien,
    string fd_tgl_lahir,
    string fs_jns_kelamin,
    string fs_nm_tipe_jaminan)
{
    public PolisView ToModel()
    {
        var result = new PolisView(
            fs_kd_polis, fs_no_polis, fs_atas_nama,
            new PasienReff(fs_mr, fs_nm_pasien, DateOnly.Parse(fd_tgl_lahir), fs_jns_kelamin),
            new TipeJaminanReff(fs_kd_tipe_jaminan, fs_nm_tipe_jaminan),
            DateOnly.Parse(fd_tgl_expired));
        return result;
    }
}