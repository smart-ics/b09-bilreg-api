using Bilreg.Domain.BrgContext.KlasifikasiFeature;

namespace Bilreg.Infrastructure.BrgContext.KlasifikasiFeature;

public record SifatDto(string fs_kd_sifat, string fs_nm_sifat)
{
    public static SifatDto FromModel(SifatType model)
    {
        return new SifatDto(model.SifatId, model.SifatName);
    }

    public SifatType ToModel()
    {
        return SifatType.Create(fs_kd_sifat, fs_nm_sifat);
    }
}
