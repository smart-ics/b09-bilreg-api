using Bilreg.Domain.BrgContext.StandardFeature;

namespace Bilreg.Infrastructure.BrgContext.StandardFeature;

public record OriginalDto(string fs_kd_original, string fs_nm_original)
{
    public static OriginalDto FromModel(OriginalType model)
    {
        return new OriginalDto(model.OriginalId, model.OriginalName);
    }

    public OriginalType ToModel()
    {
        return OriginalType.Create(fs_kd_original, fs_nm_original);
    }
}
