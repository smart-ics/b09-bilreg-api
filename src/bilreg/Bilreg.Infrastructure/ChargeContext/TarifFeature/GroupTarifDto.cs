using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

// resharper disable inconsistentnaming
public record GroupTarifDto(string fs_kd_grup_tarif, string fs_nm_grup_tarif)
{
    public static GroupTarifDto FromModel(GroupTarifType model)
    {
        var result = new GroupTarifDto(model.GroupTarifId, model.GroupTarifName);
        return result;
    }

    public GroupTarifType ToModel()
    {
        var result = new GroupTarifType(fs_kd_grup_tarif, fs_nm_grup_tarif);
        return result;
    }
}