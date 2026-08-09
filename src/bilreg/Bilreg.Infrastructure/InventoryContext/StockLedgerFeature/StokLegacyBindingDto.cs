using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StokLegacyBindingDto(
    string BindingId,
    int BindingKind,
    string StokMutasiId,
    string StokLokasiId,
    string LegacyBukuId,
    string LegacyStokId,
    string TrsReffId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public static StokLegacyBindingDto FromModel(
        StockLegacyBindingModel model,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null)
    {
        var empty = StockLedgerSentinel.EmptyDate;
        return new StokLegacyBindingDto(
            model.BindingId,
            (int)model.BindingKind,
            model.StokMutasiId,
            model.StokLokasiId,
            model.LegacyBukuId,
            model.LegacyStokId,
            model.TrsReffId,
            crtUser,
            crtDate ?? empty,
            updUser,
            updDate ?? empty);
    }

    public StockLegacyBindingModel ToModel() =>
        new(
            BindingId,
            (BindingKindEnum)BindingKind,
            StokMutasiId,
            StokLokasiId,
            LegacyBukuId,
            LegacyStokId,
            TrsReffId);
}
