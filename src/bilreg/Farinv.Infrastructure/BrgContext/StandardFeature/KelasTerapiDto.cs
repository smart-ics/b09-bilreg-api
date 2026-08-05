using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KelasTerapiDto(string fs_kd_kelas_terapi, string fs_nm_kelas_terapi)
{
    public static KelasTerapiDto FromModel(KelasTerapiType model)
    {
        return new KelasTerapiDto(model.KelasTerapiId, model.KelasTerapiName);
    }

    public KelasTerapiType ToModel()
    {
        return KelasTerapiType.Create(fs_kd_kelas_terapi, fs_nm_kelas_terapi);
    }
}