using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record PabrikDto(
    string fs_kd_pabrik,
    string fs_nm_pabrik,
    decimal fn_koef_formula)
{
    public static PabrikDto FromModel(PabrikType model)
    {
        return new PabrikDto(
            model.PabrikId,
            model.PabrikName,
            model.KoefFormula);
    }

    public PabrikType ToModel()
    {
        return PabrikType.Create(
            fs_kd_pabrik,
            fs_nm_pabrik);
    }
}