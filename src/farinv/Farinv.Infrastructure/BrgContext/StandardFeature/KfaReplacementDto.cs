using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KfaReplacementDto(string KfaId, string KfaNewId, string KfaNewName, string Reason
)
{
    public static KfaReplacementDto FromModel(KfaReplacementType model)
    {
        return new KfaReplacementDto(
            model.KfaId, model.KfaNewId, model.KfaNewName, model.Reason
        );
    }

    public KfaReplacementType ToModel()
    {
        return new KfaReplacementType(
            KfaId, KfaNewId, KfaNewName, Reason
        );
    }
}