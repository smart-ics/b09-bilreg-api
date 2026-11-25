using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameAdjustmentDto(
    string AdjustmentId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string LayananId,
    decimal AdjustmentQty,
    string BaseSatuanId,
    decimal Hpp,
    decimal NilaiFinansial,
    string StokMutasiId,
    string LegacyBukuId,
    int MovementKind,
    DateTime ExecutedAt,
    string ExecutedBy)
{
    public static StockOpnameAdjustmentDto FromModel(StockOpnameAdjustmentModel model) =>
        new(
            AdjustmentId: model.AdjustmentId,
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BatchNo: model.BatchNo,
            ExpiredDate: model.ExpiredDate,
            LayananId: model.LayananId,
            AdjustmentQty: model.AdjustmentQty,
            BaseSatuanId: model.BaseSatuanId,
            Hpp: model.Hpp,
            NilaiFinansial: model.NilaiFinansial,
            StokMutasiId: model.StokMutasiId,
            LegacyBukuId: model.LegacyBukuId,
            MovementKind: model.MovementKind,
            ExecutedAt: model.ExecutedAt,
            ExecutedBy: model.ExecutedBy
        );

    public StockOpnameAdjustmentModel ToModel() =>
        new(
            adjustmentId: AdjustmentId,
            sessionId: SessionId,
            brgId: BrgId,
            batchNo: BatchNo,
            expiredDate: ExpiredDate,
            layananId: LayananId,
            adjustmentQty: AdjustmentQty,
            baseSatuanId: BaseSatuanId,
            hpp: Hpp,
            nilaiFinansial: NilaiFinansial,
            stokMutasiId: StokMutasiId,
            legacyBukuId: LegacyBukuId,
            movementKind: MovementKind,
            executedAt: ExecutedAt,
            executedBy: ExecutedBy
        );
}
