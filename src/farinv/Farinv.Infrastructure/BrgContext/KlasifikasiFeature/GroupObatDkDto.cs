using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record GroupObatDkDto(string fs_kd_gol_obat_dk, string fs_nm_gol_obat_dk)
{
    public static GroupObatDkDto FromModel(GroupObatDkType model)
    {
        return new GroupObatDkDto(model.GroupObatDkId, model.GroupObatDkName);
    }

    public GroupObatDkType ToModel()
    {
        return GroupObatDkType.Create(fs_kd_gol_obat_dk, fs_nm_gol_obat_dk);
    }
}