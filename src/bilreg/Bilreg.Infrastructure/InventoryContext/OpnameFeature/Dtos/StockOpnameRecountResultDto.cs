using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameRecountResultDto(
    string RecountResultId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal RecountQty,
    DateTime RecountTimestamp,
    decimal SystemQtyAtRecount,
    decimal ResidualVarianceQty,
    string? DiscrepancyReason,
    string? SupervisorNotes,
    string SupervisorUserId)
{
    // Computed property for backwards compatibility with legacy callers
    public string VarianceItemId => $"{SessionId}_{BrgId}_{BatchNo}_{ExpiredDate}";

    // Backwards compatibility constructor for tests / legacy callers until P2-S03
    [Obsolete("Use primary constructor matching FARPU_StockOpnameRecountResult schema")]
    public StockOpnameRecountResultDto(
        string recountResultId,
        string varianceItemId,
        decimal recountQty,
        decimal reconstructedSystemQty,
        decimal finalVarianceQty,
        decimal finalValuation,
        int discrepancyReason,
        string notes,
        string recountedBy,
        DateTime recountTime)
        : this(
            recountResultId,
            ExtractPart(varianceItemId, 0),
            ExtractPart(varianceItemId, 1),
            ExtractPart(varianceItemId, 2),
            ExtractPart(varianceItemId, 3),
            recountQty,
            recountTime,
            reconstructedSystemQty,
            finalVarianceQty,
            ((DiscrepancyReasonEnum)discrepancyReason).ToString(),
            notes,
            recountedBy)
    {
    }

    private static string ExtractPart(string compositeKey, int index)
    {
        var parts = compositeKey.Split('_');
        return parts.Length > index ? parts[index] : string.Empty;
    }

    public static StockOpnameRecountResultDto FromModel(
        StockOpnameRecountResultModel model,
        string? sessionId = null,
        string? brgId = null,
        string? batchNo = null,
        string? expiredDate = null)
    {
        var parts = model.VarianceItemId?.Split('_') ?? [];
        var sid = !string.IsNullOrWhiteSpace(sessionId) ? sessionId : (parts.Length > 0 ? parts[0] : string.Empty);
        var bid = !string.IsNullOrWhiteSpace(brgId) ? brgId : (parts.Length > 1 ? parts[1] : string.Empty);
        var bno = !string.IsNullOrWhiteSpace(batchNo) ? batchNo : (parts.Length > 2 ? parts[2] : string.Empty);
        var exp = !string.IsNullOrWhiteSpace(expiredDate) ? expiredDate : (parts.Length > 3 ? parts[3] : string.Empty);

        return new StockOpnameRecountResultDto(
            RecountResultId: model.RecountResultId,
            SessionId: sid,
            BrgId: bid,
            BatchNo: bno,
            ExpiredDate: exp,
            RecountQty: model.RecountQty,
            RecountTimestamp: model.RecountTime,
            SystemQtyAtRecount: model.ReconstructedSystemQty,
            ResidualVarianceQty: model.FinalVarianceQty,
            DiscrepancyReason: model.DiscrepancyReason.ToString(),
            SupervisorNotes: model.Notes,
            SupervisorUserId: model.RecountedBy
        );
    }

    public StockOpnameRecountResultModel ToModel()
    {
        return new StockOpnameRecountResultModel(
            recountResultId: RecountResultId,
            varianceItemId: VarianceItemId,
            recountQty: RecountQty,
            reconstructedSystemQty: SystemQtyAtRecount,
            finalVarianceQty: ResidualVarianceQty,
            finalValuation: 0m,
            discrepancyReason: ParseReason(DiscrepancyReason),
            notes: SupervisorNotes ?? string.Empty,
            recountedBy: SupervisorUserId ?? string.Empty,
            recountTime: RecountTimestamp
        );
    }

    private static DiscrepancyReasonEnum ParseReason(string? reasonStr)
    {
        if (string.IsNullOrWhiteSpace(reasonStr))
            return DiscrepancyReasonEnum.ADMIN_ERROR;

        var normalized = reasonStr.Replace("_", "").ToLowerInvariant();
        if (normalized.Contains("damaged") || normalized.Contains("expired"))
            return DiscrepancyReasonEnum.DAMAGED_EXPIRED;
        if (normalized.Contains("misplace"))
            return DiscrepancyReasonEnum.MISPLACED;
        if (normalized.Contains("theft") || normalized.Contains("unknown"))
            return DiscrepancyReasonEnum.UNKNOWN_THEFT;
        if (normalized.Contains("admin"))
            return DiscrepancyReasonEnum.ADMIN_ERROR;

        if (Enum.TryParse<DiscrepancyReasonEnum>(reasonStr, true, out var parsed))
            return parsed;

        return DiscrepancyReasonEnum.ADMIN_ERROR;
    }
}
