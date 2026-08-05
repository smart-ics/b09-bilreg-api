using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record GenerikDto(string fs_kd_generik, string fs_nm_generik, string fs_komposisi)
{
    public static GenerikDto FromModel(GenerikType model)
    {
        var result = new GenerikDto(model.GenerikId, model.GenerikName, model.Komposisi);
        return result;
    }

    public GenerikType ToModel()
    {
        var result = new GenerikType(fs_kd_generik, fs_nm_generik, fs_komposisi);
        return result;
    }
}