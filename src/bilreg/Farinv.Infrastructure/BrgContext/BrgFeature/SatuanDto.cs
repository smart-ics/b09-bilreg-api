using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record SatuanDto(string fs_kd_satuan, string fs_nm_satuan)
{
    public static SatuanDto FromModel(SatuanType model)
    {
        return new SatuanDto(model.SatuanId, model.SatuanName);
    }

    public SatuanType ToModel()
    {
        return SatuanType.Create(fs_kd_satuan, fs_nm_satuan);
    }
}
