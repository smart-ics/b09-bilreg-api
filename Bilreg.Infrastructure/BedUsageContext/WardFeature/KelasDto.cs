using Bilreg.Domain.BedUsageContext.WardFeature;


// Resharper disable InconsistentNaming
namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public record KelasDto(
    string fs_kd_kelas, string fs_nm_kelas, bool fb_aktif,
    string fs_kd_kelas_dk, string fs_nm_kelas_dk)
{
    public static KelasDto FromModel(KelasType model)
    {
        var result = new KelasDto(model.KelasId, model.KelasName,
            model.IsAktif, model.KelasDk.KelasDkId, model.KelasDk.KelasDkName);
        return result;
    }

    public KelasType ToModel()
    {
        var result = new KelasType(
            fs_kd_kelas, fs_nm_kelas, fb_aktif,
            new KelasDkType(fs_kd_kelas_dk, fs_nm_kelas_dk));
        return result;
    }
}