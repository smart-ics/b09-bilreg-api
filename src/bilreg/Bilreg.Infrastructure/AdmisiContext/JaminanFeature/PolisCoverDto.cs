using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public record PolisCoverDto(
    string fs_kd_polis,
    string fs_mr,
    string fs_kd_status,
    string fs_nm_pasien,
    string fd_tgl_lahir,
    string fs_jns_kelamin)
{
    public static PolisCoverDto FromModel (PolisCoverModel model)
    {
        return new PolisCoverDto(
            model.PolisId,
            model.Pasien.PasienId,
            model.Status.StatusCode,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender);
    }

    public PolisCoverModel ToModel()
    {
        var result = new PolisCoverModel(fs_kd_polis,
            new PasienReff(fs_mr, fs_nm_pasien, DateOnly.Parse(fd_tgl_lahir), fs_jns_kelamin),
            StatusPesertaType.Create(fs_kd_status));
        return result;
    }
};