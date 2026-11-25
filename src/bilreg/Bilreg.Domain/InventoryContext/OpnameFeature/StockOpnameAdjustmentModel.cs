using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameAdjustmentModel
{
    #region PROPERTIES
    public string AdjustmentId { get; private set; }
    public string SessionId { get; private set; }
    public string BrgId { get; private set; }
    public string BatchNo { get; private set; }
    public string ExpiredDate { get; private set; }
    public string LayananId { get; private set; }
    public decimal AdjustmentQty { get; private set; }
    public string BaseSatuanId { get; private set; }
    public decimal Hpp { get; private set; }
    public decimal NilaiFinansial { get; private set; }
    public string StokMutasiId { get; private set; }
    public string LegacyBukuId { get; private set; }
    public int MovementKind { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public string ExecutedBy { get; private set; }

    public ItemTupleKey TupleKey => new(BrgId, BatchNo, ExpiredDate);
    #endregion

    #region CREATION
    public StockOpnameAdjustmentModel(
        string adjustmentId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string layananId,
        decimal adjustmentQty,
        string baseSatuanId,
        decimal hpp,
        decimal nilaiFinansial,
        string stokMutasiId,
        string legacyBukuId,
        int movementKind,
        DateTime executedAt,
        string executedBy)
    {
        AdjustmentId = adjustmentId?.Trim() ?? string.Empty;
        SessionId = sessionId?.Trim() ?? string.Empty;
        BrgId = brgId?.Trim() ?? string.Empty;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        LayananId = layananId?.Trim() ?? string.Empty;
        AdjustmentQty = adjustmentQty;
        BaseSatuanId = baseSatuanId?.Trim() ?? string.Empty;
        Hpp = hpp;
        NilaiFinansial = nilaiFinansial;
        StokMutasiId = stokMutasiId?.Trim() ?? string.Empty;
        LegacyBukuId = legacyBukuId?.Trim() ?? string.Empty;
        MovementKind = movementKind;
        ExecutedAt = executedAt;
        ExecutedBy = executedBy?.Trim() ?? string.Empty;
    }

    public static StockOpnameAdjustmentModel Create(
        string adjustmentId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string layananId,
        decimal adjustmentQty,
        string baseSatuanId,
        decimal hpp,
        string stokMutasiId,
        string legacyBukuId,
        int movementKind,
        DateTime executedAt,
        string executedBy)
    {
        Guard.Against.NullOrWhiteSpace(adjustmentId, nameof(adjustmentId));
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(executedBy, nameof(executedBy));
        var nilaiFinansial = adjustmentQty * hpp;
        return new StockOpnameAdjustmentModel(
            adjustmentId,
            sessionId,
            brgId,
            batchNo,
            expiredDate,
            layananId,
            adjustmentQty,
            baseSatuanId,
            hpp,
            nilaiFinansial,
            stokMutasiId,
            legacyBukuId,
            movementKind,
            executedAt,
            executedBy);
    }
    #endregion
}
