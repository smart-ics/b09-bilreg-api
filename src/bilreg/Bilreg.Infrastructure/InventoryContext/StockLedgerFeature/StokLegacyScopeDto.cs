using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StokLegacyScopeDto(
    string BrgId,
    string BrgMasukReffId,
    int AlignmentStatus,
    DateTime TglMutasiLast,
    string LastLegacyBukuId,
    DateTime LastSyncedAt,
    string InconsistencyReason,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate)
{
    public static StokLegacyScopeDto FromModel(
        StockLegacyScopeModel model,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null)
    {
        var empty = StockLedgerSentinel.EmptyDate;
        return new StokLegacyScopeDto(
            model.BrgId,
            model.BrgMasukReffId,
            (int)model.AlignmentStatus,
            model.TglMutasiLast,
            model.LastLegacyBukuId,
            model.LastSyncedAt,
            model.InconsistencyReason,
            crtUser,
            crtDate ?? empty,
            updUser,
            updDate ?? empty);
    }

    public StockLegacyScopeModel ToModel() =>
        new(
            BrgId,
            BrgMasukReffId,
            (AlignmentStatusEnum)AlignmentStatus,
            TglMutasiLast,
            LastLegacyBukuId,
            LastSyncedAt,
            InconsistencyReason);
}
