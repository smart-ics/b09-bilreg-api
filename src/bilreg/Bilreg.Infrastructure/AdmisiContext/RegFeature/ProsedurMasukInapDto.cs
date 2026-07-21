using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record ProsedurMasukInapDto(
    string fs_kd_caramasuk_inap,
    string fs_nm_caramasuk_inap,
    string fs_kd_caramasuk_inap_dk,
    string fs_nm_caramasuk_inap_dk)
{
    public static ProsedurMasukInapDto FromModel(ProsedurMasukInapType model)
    {
        var result = new ProsedurMasukInapDto(
            model.ProsedurMasukInapId,
            model.ProsedurMasukInapName,
            model.ProsedurMasukInapDk.ProsedurMasukInapDkId,
            model.ProsedurMasukInapDk.ProsedurMasukInapDkName);
        return result;
    }

    public ProsedurMasukInapType ToModel()
    {
        var dk = new ProsedurMasukInapDkType(fs_kd_caramasuk_inap_dk, fs_nm_caramasuk_inap_dk);
        var result = new ProsedurMasukInapType(
            fs_kd_caramasuk_inap,
            fs_nm_caramasuk_inap, dk);
        return result;
    }
}
