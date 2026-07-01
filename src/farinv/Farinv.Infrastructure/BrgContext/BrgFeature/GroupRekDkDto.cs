using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record GroupRekDkDto(string fs_kd_grup_rek_dk, string fs_nm_grup_rek_dk)
{
    public static GroupRekDkDto FromModel(GroupRekDkType model)
    {
        var result = new GroupRekDkDto(model.GroupRekDkId, model.GroupRekDkName);
        return result;
    }

    public GroupRekDkType ToModel()
    {
        var result = new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk);
        return result;
    }
}