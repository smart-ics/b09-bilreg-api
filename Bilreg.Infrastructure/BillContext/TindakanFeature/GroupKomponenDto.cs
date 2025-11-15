using Bilreg.Domain.BillContext.TindakanFeature;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature.TindakanAgg;

// resharper disable inconsistentnaming
public record GroupKomponenDto(string fs_kd_grup_detil_tarif, string fs_nm_grup_detil_tarif)
{
    public static GroupKomponenDto FromModel(GroupKomponenType model)
    {
        var result = new GroupKomponenDto(model.GroupKomponenId, model.GroupKomponenName);
        return result;
    }

    public GroupKomponenType ToModel()
    {
        var result = new GroupKomponenType(fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif);
        return result;
    }
}