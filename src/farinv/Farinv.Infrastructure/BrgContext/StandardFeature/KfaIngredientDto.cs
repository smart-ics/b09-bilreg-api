using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KfaIngredientDto(string KfaId, 
    string KfaIngredientId, string KfaIngredientName,
    bool Active, string State, string Strength
)
{
    public static KfaIngredientDto FromModel(KfaIngredientType model)
    {
        return new KfaIngredientDto(model.KfaId,
            model.KfaIngredientId, model.KfaIngredientName,
            model.Active, model.State, model.Strength
        );
    }

    public KfaIngredientType ToModel()
    {
        return new KfaIngredientType( KfaId,
            KfaIngredientId, KfaIngredientName,
            Active, State, Strength
        );
    }
}
