using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StokLokasiDto(
    string StokLokasiId,
    string StokBatchId,
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    DateTime TglEd,
    string NoBatch,
    decimal QtySisa,
    long Version,
    DateTime TglMasuk,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public static StokLokasiDto FromModel(
        LocationStockBalanceModel model,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null)
    {
        var empty = StockLedgerSentinel.EmptyDate;
        return new StokLokasiDto(
            model.StokLokasiId,
            model.StokBatchId,
            model.BrgId,
            model.BrgMasukReffId,
            model.LayananId,
            model.TglEd,
            model.NoBatch,
            model.QtySisa,
            model.Version,
            model.TglMasuk,
            crtUser,
            crtDate ?? empty,
            updUser,
            updDate ?? empty);
    }

    public LocationStockBalanceModel ToModel() =>
        new(
            StokLokasiId,
            StokBatchId,
            BrgId,
            BrgMasukReffId,
            LayananId,
            TglEd,
            NoBatch,
            TglMasuk,
            QtySisa,
            Version);
}
