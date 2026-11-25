using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameRecountResultModel
{
    #region PROPERTIES
    public string RecountResultId { get; private set; }
    public string VarianceItemId { get; private set; }
    public decimal RecountQty { get; private set; }
    public decimal ReconstructedSystemQty { get; private set; }
    public decimal FinalVarianceQty { get; private set; }
    public decimal FinalValuation { get; private set; }
    public DiscrepancyReasonEnum DiscrepancyReason { get; private set; }
    public string Notes { get; private set; }
    public string RecountedBy { get; private set; }
    public DateTime RecountTime { get; private set; }
    #endregion

    #region CREATION
    public StockOpnameRecountResultModel(
        string recountResultId,
        string varianceItemId,
        decimal recountQty,
        decimal reconstructedSystemQty,
        decimal finalVarianceQty,
        decimal finalValuation,
        DiscrepancyReasonEnum discrepancyReason,
        string notes,
        string recountedBy,
        DateTime recountTime)
    {
        RecountResultId = recountResultId?.Trim() ?? string.Empty;
        VarianceItemId = varianceItemId?.Trim() ?? string.Empty;
        RecountQty = recountQty;
        ReconstructedSystemQty = reconstructedSystemQty;
        FinalVarianceQty = finalVarianceQty;
        FinalValuation = finalValuation;
        DiscrepancyReason = discrepancyReason;
        Notes = notes?.Trim() ?? string.Empty;
        RecountedBy = recountedBy?.Trim() ?? string.Empty;
        RecountTime = recountTime;
    }

    public static StockOpnameRecountResultModel Create(
        string recountResultId,
        string varianceItemId,
        decimal recountQty,
        decimal reconstructedSystemQty,
        decimal hpp,
        DiscrepancyReasonEnum discrepancyReason,
        string notes,
        string recountedBy,
        DateTime recountTime)
    {
        Guard.Against.NullOrWhiteSpace(varianceItemId, nameof(varianceItemId));
        Guard.Against.NullOrWhiteSpace(recountedBy, nameof(recountedBy));
        var finalVarianceQty = recountQty - reconstructedSystemQty;
        var finalValuation = finalVarianceQty * hpp;
        return new StockOpnameRecountResultModel(
            recountResultId,
            varianceItemId,
            recountQty,
            reconstructedSystemQty,
            finalVarianceQty,
            finalValuation,
            discrepancyReason,
            notes,
            recountedBy,
            recountTime);
    }
    #endregion
}
