using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record GroupRekDto(string fs_kd_grup_rek, string fs_nm_grup_rek, string fs_kd_grup_rek_dk, string fs_nm_grup_rek_dk)
{
    public static GroupRekDto FromModel(GroupRekType model)
    {
        var result = new GroupRekDto(
            model.GroupRekId, 
            model.GroupRekName, 
            model.GroupRekDk.GroupRekDkId, 
            model.GroupRekDk.GroupRekDkName);
        return result;
    }

    public GroupRekType ToModel()
    {
        var groupRekDk = new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk);
        var result = new GroupRekType(fs_kd_grup_rek, fs_nm_grup_rek, groupRekDk);
        return result;
    }
}