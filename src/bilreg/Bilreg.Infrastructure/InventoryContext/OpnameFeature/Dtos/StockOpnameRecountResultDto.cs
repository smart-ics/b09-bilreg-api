using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameRecountResultDto(
    string RecountResultId,
    string VarianceItemId,
    decimal RecountQty,
    decimal ReconstructedSystemQty,
    decimal FinalVarianceQty,
    decimal FinalValuation,
    int DiscrepancyReason,
    string Notes,
    string RecountedBy,
    DateTime RecountTime)
{
    public static StockOpnameRecountResultDto FromModel(StockOpnameRecountResultModel model) =>
        new(
            RecountResultId: model.RecountResultId,
            VarianceItemId: model.VarianceItemId,
            RecountQty: model.RecountQty,
            ReconstructedSystemQty: model.ReconstructedSystemQty,
            FinalVarianceQty: model.FinalVarianceQty,
            FinalValuation: model.FinalValuation,
            DiscrepancyReason: (int)model.DiscrepancyReason,
            Notes: model.Notes,
            RecountedBy: model.RecountedBy,
            RecountTime: model.RecountTime
        );

    public StockOpnameRecountResultModel ToModel() =>
        new(
            recountResultId: RecountResultId,
            varianceItemId: VarianceItemId,
            recountQty: RecountQty,
            reconstructedSystemQty: ReconstructedSystemQty,
            finalVarianceQty: FinalVarianceQty,
            finalValuation: FinalValuation,
            discrepancyReason: (DiscrepancyReasonEnum)DiscrepancyReason,
            notes: Notes ?? string.Empty,
            recountedBy: RecountedBy ?? string.Empty,
            recountTime: RecountTime
        );
}
