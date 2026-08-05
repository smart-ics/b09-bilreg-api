using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record KelompokDto(string fs_kd_kelompok, string fs_nm_kelompok)
{
    public static KelompokDto FromModel(KelompokType model)
    {
        return new KelompokDto(model.KelompokId, model.KelompokName);
    }

    public KelompokType ToModel()
    {
        return KelompokType.Create(fs_kd_kelompok, fs_nm_kelompok);
    }
}