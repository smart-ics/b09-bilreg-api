using Bilreg.Domain.BillContext.BedUsageFeature;

namespace Bilreg.Infrastructure.BillContext.BedUsageFeature;
// Resharper disable InconsistentNaming
public record KelasDkDto(string fs_kd_kelas_dk, string fs_nm_kelas_dk)
{
    public static KelasDkDto FromModel(KelasDkType model)
        => new KelasDkDto(model.KelasDkId, model.KelasDkName);

    public KelasDkType ToModel()
    {
        var result = new KelasDkType(fs_kd_kelas_dk, fs_nm_kelas_dk);
        return result;
    }
}