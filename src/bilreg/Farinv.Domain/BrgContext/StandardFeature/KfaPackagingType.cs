using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KfaPackagingType : IKfaKey
{
    #region CREATION
    public KfaPackagingType( string kfaId,
        string kfaPackagingId, string kfaPackagingName,
        decimal packPrice, string uomId,
        decimal qty, decimal generate)
    {
        KfaId = kfaId;
        KfaPackagingId = kfaPackagingId;
        KfaPackagingName = kfaPackagingName;
        PackPrice = packPrice;
        UomId = uomId;
        Qty = qty;
        Generate = generate;
    }

    public static KfaPackagingType Default => new("-", "-", "-", 0, "-", 0, 0);
    #endregion

    #region PROPERTIES
    public string KfaId { get; init; }
    public string KfaPackagingId { get; init; }
    public string KfaPackagingName { get; init; }
    public decimal PackPrice { get; init; }
    public string UomId { get; init; }
    public decimal Qty { get; init; }
    public decimal Generate { get; init; }
    #endregion
}