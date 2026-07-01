using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record FormulariumDto(string fs_kd_formularium, string fs_nm_formularium)
{
    public static FormulariumDto FromModel(FormulariumType model)
    {
        var result = new FormulariumDto(
            fs_kd_formularium: model.FormulariumId,
            fs_nm_formularium: model.FormulariumName);
        return result;
    }

    public FormulariumType ToModel()
    {
        var result = FormulariumType.Create(
            formulariumId: fs_kd_formularium,
            formulariumName: fs_nm_formularium);
        return result;
    }
}
