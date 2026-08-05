using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record GolTerapiDto(string fs_kd_gol_terapi, string fs_nm_gol_terapi)
{
    public static GolTerapiDto FromModel(GolTerapiType model)
    {
        return new GolTerapiDto(model.GolTerapiId, model.GolTerapiName);
    }

    public GolTerapiType ToModel()
    {
        return GolTerapiType.Create(fs_kd_gol_terapi, fs_nm_gol_terapi);
    }
}
