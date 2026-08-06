using Bilreg.Domain.BrgContext.KlasifikasiFeature;

namespace Bilreg.Infrastructure.BrgContext.KlasifikasiFeature;

public record BentukDto(string fs_kd_bentuk, string fs_nm_bentuk)
{
    public static BentukDto FromModel(BentukType model)
    {
        return new BentukDto(model.BentukId, model.BentukName);
    }

    public BentukType ToModel()
    {
        return BentukType.Create(fs_kd_bentuk, fs_nm_bentuk);
    }
}