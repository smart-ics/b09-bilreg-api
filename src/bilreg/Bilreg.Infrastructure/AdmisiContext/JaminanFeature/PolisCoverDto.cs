using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public record PolisCoverDto(
    string fs_kd_polis,
    string fs_mr,
    string fs_kd_status,
    string fs_nm_pasien,
    string fd_tgl_lahir,
    string fs_jns_kelamin,
    string fd_tgl_expired = "3000-01-01",
    string crttgl = "3000-01-01",
    string crtjam = "00:00:00",
    string crtipa = "",
    string crtver = "",
    string crtusr = "",
    string updtgl = "3000-01-01",
    string updjam = "00:00:00",
    string updipa = "",
    string updver = "",
    string updusr = "")
{
    public static PolisCoverDto FromModel(PolisCoverModel model, DateOnly? expiredDate = null)
    {
        var now = DateTime.Now;
        var tglExpired = (expiredDate ?? new DateOnly(3000, 1, 1)).ToString("yyyy-MM-dd");
        return new PolisCoverDto(
            model.PolisId,
            model.Pasien.PasienId,
            model.Status.StatusCode,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            fd_tgl_expired: tglExpired,
            crttgl: now.ToString("yyyy-MM-dd"),
            crtjam: now.ToString("HH:mm:ss"),
            crtipa: "",
            crtver: "",
            crtusr: "",
            updtgl: "3000-01-01",
            updjam: "00:00:00",
            updipa: "",
            updver: "",
            updusr: "");
    }

    public PolisCoverModel ToModel()
    {
        var result = new PolisCoverModel(fs_kd_polis,
            new PasienReff(fs_mr, fs_nm_pasien, DateOnly.Parse(fd_tgl_lahir), fs_jns_kelamin),
            StatusPesertaType.Create(fs_kd_status));
        return result;
    }
};