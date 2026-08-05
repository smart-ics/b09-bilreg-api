using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record KfaDosageUsageType : IKfaKey
{
    #region CREATION
    public KfaDosageUsageType( string kfaId, string displayName, 
        string category, decimal bodyWeightMax, decimal bodyWeightMin,
        decimal duration, decimal durationMax, string durationUcum,
        decimal frequency, decimal frequencyMax, decimal period,
        string periodUcum, decimal qty, decimal qtyHigh,
        string qtyUcum, string qtyUom, bool useUcum)
    {
        KfaId = kfaId;
        DisplayName = displayName;
        Category = category;
        BodyWeightMax = bodyWeightMax;
        BodyWeightMin = bodyWeightMin;
        Duration = duration;
        DurationMax = durationMax;
        DurationUcum = durationUcum;
        Frequency = frequency;
        FrequencyMax = frequencyMax;
        Period = period;
        PeriodUcum = periodUcum;
        Qty = qty;
        QtyHigh = qtyHigh;
        QtyUcum = qtyUcum;
        QtyUom = qtyUom;
        UseUcum = useUcum;
    }

    public static KfaDosageUsageType Default => new(
        "-", "-", "-", 0, 0, 0, 0, "-", 0, 0, 0, "-", 0, 0, "-", "-", false);
    #endregion

    #region PROPERTIES
    public string KfaId { get; init; }
    public string DisplayName { get; init; }
    public string Category { get; init; }
    public decimal BodyWeightMax { get; init; }
    public decimal BodyWeightMin { get; init; }
    public decimal Duration { get; init; }
    public decimal DurationMax { get; init; }
    public string DurationUcum { get; init; }
    public decimal Frequency { get; init; }
    public decimal FrequencyMax { get; init; }
    public decimal Period { get; init; }
    public string PeriodUcum { get; init; }
    public decimal Qty { get; init; }
    public decimal QtyHigh { get; init; }
    public string QtyUcum { get; init; }
    public string QtyUom { get; init; }
    public bool UseUcum { get; init; }
    #endregion
}