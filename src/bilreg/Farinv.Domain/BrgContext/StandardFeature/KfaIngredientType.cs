using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KfaIngredientType : IKfaKey
{
    #region CREATION
    public KfaIngredientType(string kfaId,
        string kfaIngredientId, string kfaIngredientName,
        bool active, string state, string strength)
    {
        KfaId = kfaId;
        KfaIngredientId = kfaIngredientId;
        KfaIngredientName = kfaIngredientName;
        Active = active;
        State = state;
        Strength = strength;
    }

    public static KfaIngredientType Default => new("-", "-", "-", false, "-", "-");
    #endregion

    #region PROPERTIES
    public string KfaId { get; init; }
    public string KfaIngredientId { get; init; }
    public string KfaIngredientName { get; init; }
    public bool Active { get; init; }
    public string State { get; init; }
    public string Strength { get; init; }
    #endregion
}