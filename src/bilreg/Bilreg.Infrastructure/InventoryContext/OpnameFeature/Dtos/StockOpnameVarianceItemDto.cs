using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameVarianceItemDto(
    string VarianceItemId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal CountQty,
    decimal SystemQty,
    decimal VarianceQty,
    decimal Hpp,
    decimal VarianceValuation,
    int State)
{
    public static StockOpnameVarianceItemDto FromModel(StockOpnameVarianceItemModel model) =>
        new(
            VarianceItemId: model.VarianceItemId,
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BatchNo: model.BatchNo,
            ExpiredDate: model.ExpiredDate,
            BaseSatuanId: model.BaseSatuanId,
            CountQty: model.CountQty,
            SystemQty: model.SystemQty,
            VarianceQty: model.VarianceQty,
            Hpp: model.Hpp,
            VarianceValuation: model.VarianceValuation,
            State: model.State
        );

    public StockOpnameVarianceItemModel ToModel(StockOpnameRecountResultModel? recountResult = null) =>
        new(
            varianceItemId: VarianceItemId,
            sessionId: SessionId,
            brgId: BrgId,
            batchNo: BatchNo,
            expiredDate: ExpiredDate,
            baseSatuanId: BaseSatuanId,
            countQty: CountQty,
            systemQty: SystemQty,
            varianceQty: VarianceQty,
            hpp: Hpp,
            varianceValuation: VarianceValuation,
            state: State,
            recountResult: recountResult
        );
}
