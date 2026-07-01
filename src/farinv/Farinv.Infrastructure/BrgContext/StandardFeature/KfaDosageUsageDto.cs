using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record KfaDosageUsageDto(string KfaId, string DisplayName,
    string Category, decimal BodyWeightMax, decimal BodyWeightMin, decimal Duration,
    decimal DurationMax, string DurationUcum, decimal Frequency, decimal FrequencyMax,
    decimal Period, string PeriodUcum, decimal Qty, decimal QtyHigh,
    string QtyUcum, string QtyUom, bool UseUcum
)
{
    public static KfaDosageUsageDto FromModel(KfaDosageUsageType model)
    {
        return new KfaDosageUsageDto(model.KfaId,model.DisplayName,
            model.Category,model.BodyWeightMax,model.BodyWeightMin, model.Duration,
            model.DurationMax, model.DurationUcum, model.Frequency, model.FrequencyMax,
            model.Period, model.PeriodUcum, model.Qty, model.QtyHigh,
            model.QtyUcum, model.QtyUom, model.UseUcum
        );
    }

    public KfaDosageUsageType ToModel()
    {
        return new KfaDosageUsageType(KfaId, DisplayName,
            Category, BodyWeightMax, BodyWeightMin, Duration,
            DurationMax, DurationUcum, Frequency, FrequencyMax,
            Period, PeriodUcum, Qty, QtyHigh,
            QtyUcum, QtyUom, UseUcum
        );
    }
}