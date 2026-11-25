using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameVarianceItemDto(
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal PhysicalCountQty,
    decimal SystemQtyAtCount,
    decimal InitialVarianceQty,
    string VerificationStatus,
    decimal? FinalQuantity = null)
{
    // Computed property for backwards compatibility with legacy callers
    public string VarianceItemId => $"{SessionId}_{BrgId}_{BatchNo}_{ExpiredDate}";

    // Backwards compatibility constructor for tests / legacy callers until P2-S03
    [Obsolete("Use primary constructor matching FARPU_StockOpnameVarianceItem schema")]
    public StockOpnameVarianceItemDto(
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
        int state)
        : this(
            sessionId,
            brgId,
            batchNo,
            expiredDate,
            baseSatuanId,
            countQty,
            systemQty,
            varianceQty,
            state == 1 ? "Verified" : "PendingVerification",
            null)
    {
    }

    public static StockOpnameVarianceItemDto FromModel(StockOpnameVarianceItemModel model) =>
        new(
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BatchNo: model.BatchNo,
            ExpiredDate: model.ExpiredDate,
            BaseSatuanId: model.BaseSatuanId,
            PhysicalCountQty: model.CountQty,
            SystemQtyAtCount: model.SystemQty,
            InitialVarianceQty: model.VarianceQty,
            VerificationStatus: model.IsVerified ? "Verified" : "PendingVerification",
            FinalQuantity: model.IsVerified ? (model.RecountResult != null ? model.RecountResult.RecountQty : model.CountQty) : null
        );

    public StockOpnameVarianceItemModel ToModel(StockOpnameRecountResultModel? recountResult = null) =>
        new(
            varianceItemId: VarianceItemId,
            sessionId: SessionId,
            brgId: BrgId,
            batchNo: BatchNo,
            expiredDate: ExpiredDate,
            baseSatuanId: BaseSatuanId,
            countQty: PhysicalCountQty,
            systemQty: SystemQtyAtCount,
            varianceQty: InitialVarianceQty,
            hpp: 0m,
            varianceValuation: 0m,
            state: VerificationStatus == "Verified" ? 1 : 0,
            recountResult: recountResult
        );
}
