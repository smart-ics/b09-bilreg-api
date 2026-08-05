using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public record GolonganDto(string fs_kd_golongan, string fs_nm_golongan)
{
    public static GolonganDto FromModel(GolonganType model)
    {
        return new GolonganDto(model.GolonganId, model.GolonganName);
    }

    public GolonganType ToModel()
    {
        return GolonganType.Create(fs_kd_golongan, fs_nm_golongan);
    }
}