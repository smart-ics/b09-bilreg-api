using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KfaReplacementType : IKfaKey
{
    #region CREATION
    public KfaReplacementType(string kfaId,
        string kfaNewId, string kfaNewName, string reason)
    {
        KfaId = kfaId;
        KfaNewId = kfaNewId;
        KfaNewName = kfaNewName;
        Reason = reason;
    }

    public static KfaReplacementType Default => new("-", "-", "-", "-");
    #endregion

    #region PROPERTIES
    public string KfaId { get; init; }
    public string KfaNewId { get; init; }
    public string KfaNewName { get; init; }
    public string Reason { get; init; }
    #endregion
}
