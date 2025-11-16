using Bilreg.Domain.BillContext.TindakanFeature;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

// resharper disable inconsistentnaming
public record GroupTarifDkDto(string fs_kd_grup_tarif_dk, string fs_nm_grup_tarif_dk)
{
    public static GroupTarifDkDto FromModel(GroupTarifDkType model)
    {
        var result = new GroupTarifDkDto(model.GroupTarifDkId, model.GroupTarifDkName);
        return result;
    }

    public GroupTarifDkType ToModel()
    {
        var result = new GroupTarifDkType(fs_kd_grup_tarif_dk, fs_nm_grup_tarif_dk);
        return result;
    }
}