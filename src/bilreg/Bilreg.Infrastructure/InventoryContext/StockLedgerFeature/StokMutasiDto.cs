using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StokMutasiDto(
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    DateTime TglEd,
    string TrsReffId,
    int MovementKind,
    decimal QtyIn,
    decimal QtyOut,
    decimal Hpp,
    string PoReffId,
    DateTime TglMutasi,
    string ReversesMutasiId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public static StokMutasiDto FromModel(
        StockMovementModel model,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null)
    {
        var empty = StockLedgerSentinel.EmptyDate;
        return new StokMutasiDto(
            model.StokMutasiId,
            model.StokLokasiId,
            model.StokBatchId,
            model.BrgId,
            model.BrgMasukReffId,
            model.LayananId,
            model.TglEd,
            model.TrsReffId,
            (int)model.MovementKind,
            model.QtyIn,
            model.QtyOut,
            model.Hpp,
            model.PoReffId,
            model.TglMutasi,
            model.ReversesMutasiId,
            crtUser,
            crtDate ?? empty,
            updUser,
            updDate ?? empty);
    }

    public StockMovementModel ToModel() =>
        new(
            StokMutasiId,
            StokLokasiId,
            StokBatchId,
            BrgId,
            BrgMasukReffId,
            LayananId,
            TglEd,
            TrsReffId,
            (MovementKindEnum)MovementKind,
            QtyIn,
            QtyOut,
            Hpp,
            PoReffId,
            TglMutasi,
            ReversesMutasiId);
}
