using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockBatchDto(
    string StokBatchId,
    string BrgId,
    string BrgMasukReffId,
    decimal QtySisa,
    decimal Hpp,
    DateTime TglMasuk,
    string PoReffId,
    long Version,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public static StockBatchDto FromModel(
        StockBatchModel model,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null)
    {
        var empty = StockLedgerSentinel.EmptyDate;
        return new StockBatchDto(
            model.StokBatchId,
            model.BrgId,
            model.BrgMasukReffId,
            model.QtySisa,
            model.Hpp,
            model.TglMasuk,
            model.PoReffId,
            model.Version,
            crtUser,
            crtDate ?? empty,
            updUser,
            updDate ?? empty);
    }

    public StockBatchModel ToModel(IEnumerable<LocationStockBalanceModel> listLokasi) =>
        new(
            StokBatchId,
            BrgId,
            BrgMasukReffId,
            QtySisa,
            Hpp,
            TglMasuk,
            PoReffId,
            Version,
            listLokasi);
}
