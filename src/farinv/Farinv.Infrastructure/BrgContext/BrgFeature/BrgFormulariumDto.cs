using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record BrgFormulariumDto(
    string fs_kd_barang,
    string fs_kd_formularium
)
{
    public static BrgFormulariumDto FromModel(string brgId, FormulariumType formularium)
    {
        return new BrgFormulariumDto(
            fs_kd_barang: brgId,
            fs_kd_formularium: formularium.FormulariumId
        );
    }

    public FormulariumType ToModel()
    {
        // Nama formularium bisa diambil dari repo FormulariumType saat ToModel di Repo
        return FormulariumType.Create(fs_kd_formularium, "-");
    }
}