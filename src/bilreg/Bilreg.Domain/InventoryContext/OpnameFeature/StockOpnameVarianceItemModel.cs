using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameVarianceItemModel
{
    #region PROPERTIES
    public string VarianceItemId { get; private set; }
    public string SessionId { get; private set; }
    public string BrgId { get; private set; }
    public string BatchNo { get; private set; }
    public string ExpiredDate { get; private set; }
    public string BaseSatuanId { get; private set; }
    public decimal CountQty { get; private set; }
    public decimal SystemQty { get; private set; }
    public decimal VarianceQty { get; private set; }
    public decimal Hpp { get; private set; }
    public decimal VarianceValuation { get; private set; }
    public int State { get; private set; } // 0 = Unverified, 1 = Verified
    public StockOpnameRecountResultModel? RecountResult { get; private set; }

    public ItemTupleKey TupleKey => new(BrgId, BatchNo, ExpiredDate);
    public bool IsVerified => State == 1;

    public decimal FinalVarianceQty => RecountResult != null ? RecountResult.FinalVarianceQty : VarianceQty;
    public decimal FinalValuation => RecountResult != null ? RecountResult.FinalValuation : VarianceValuation;
    #endregion

    #region CREATION
    public StockOpnameVarianceItemModel(
        string varianceItemId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string baseSatuanId,
        decimal countQty,
        decimal systemQty,
        decimal varianceQty,
        decimal hpp,
        decimal varianceValuation,
        int state,
        StockOpnameRecountResultModel? recountResult = null)
    {
        VarianceItemId = varianceItemId?.Trim() ?? string.Empty;
        SessionId = sessionId?.Trim() ?? string.Empty;
        BrgId = brgId?.Trim() ?? string.Empty;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        BaseSatuanId = baseSatuanId?.Trim() ?? string.Empty;
        CountQty = countQty;
        SystemQty = systemQty;
        VarianceQty = varianceQty;
        Hpp = hpp;
        VarianceValuation = varianceValuation;
        State = state;
        RecountResult = recountResult;
    }

    public static StockOpnameVarianceItemModel Create(
        string varianceItemId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string baseSatuanId,
        decimal countQty,
        decimal systemQty,
        decimal hpp)
    {
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        var varianceQty = countQty - systemQty;
        var varianceValuation = varianceQty * hpp;
        return new StockOpnameVarianceItemModel(
            varianceItemId,
            sessionId,
            brgId,
            batchNo,
            expiredDate,
            baseSatuanId,
            countQty,
            systemQty,
            varianceQty,
            hpp,
            varianceValuation,
            state: 0);
    }
    #endregion

    #region BEHAVIOR
    public void SetRecountResult(StockOpnameRecountResultModel recountResult)
    {
        Guard.Against.Null(recountResult, nameof(recountResult));
        RecountResult = recountResult;
        State = 1;
    }

    public void MarkVerifiedWithReason(
        DiscrepancyReasonEnum reason,
        string notes,
        string verifiedBy,
        DateTime verifiedAt,
        string recountResultId = "")
    {
        Guard.Against.NullOrWhiteSpace(verifiedBy, nameof(verifiedBy));
        var result = StockOpnameRecountResultModel.Create(
            recountResultId,
            VarianceItemId,
            CountQty,
            SystemQty,
            Hpp,
            reason,
            notes,
            verifiedBy,
            verifiedAt);
        RecountResult = result;
        State = 1;
    }
    #endregion
}
