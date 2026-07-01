using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KfaPackagingDto(string KfaId,
    string KfaPackagingId, string KfaPackagingName,
    decimal PackPrice, string UomId,
    decimal Qty, decimal Generate
)
{
    public static KfaPackagingDto FromModel(KfaPackagingType model)
    {
        return new KfaPackagingDto(model.KfaId,
            model.KfaPackagingId, model.KfaPackagingName,
            model.PackPrice, model.UomId,
            model.Qty, model.Generate
        );
    }

    public KfaPackagingType ToModel()
    {
        return new KfaPackagingType(KfaId,
            KfaPackagingId, KfaPackagingName,
            PackPrice, UomId,
            Qty, Generate
        );
    }
}
