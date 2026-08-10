using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public class StockLegacyBindingModel : IStockLegacyBindingKey
{
    private const string IdPrefix = "SBD";

    #region CREATION

    public StockLegacyBindingModel(
        string bindingId,
        BindingKindEnum bindingKind,
        string stokMutasiId,
        string stokLokasiId,
        string legacyBukuId,
        string legacyStokId,
        string trsReffId)
    {
        BindingId = bindingId;
        BindingKind = bindingKind;
        StokMutasiId = stokMutasiId;
        StokLokasiId = stokLokasiId;
        LegacyBukuId = legacyBukuId;
        LegacyStokId = legacyStokId;
        TrsReffId = trsReffId;
    }

    public static StockLegacyBindingModel CreateMutasiBuku(
        string stokMutasiId,
        string legacyBukuId,
        string trsReffId,
        string? stokLokasiId = null,
        string? legacyStokId = null)
    {
        Guard.Against.NullOrWhiteSpace(stokMutasiId);
        Guard.Against.NullOrWhiteSpace(legacyBukuId);
        Guard.Against.NullOrWhiteSpace(trsReffId);

        return new StockLegacyBindingModel(
            NunaId.New(IdPrefix),
            BindingKindEnum.MutasiBuku,
            stokMutasiId,
            stokLokasiId ?? string.Empty,
            legacyBukuId,
            legacyStokId ?? string.Empty,
            trsReffId);
    }

    public static StockLegacyBindingModel CreateLokasiStok(
        string stokLokasiId,
        string legacyStokId,
        string trsReffId,
        string? stokMutasiId = null,
        string? legacyBukuId = null)
    {
        Guard.Against.NullOrWhiteSpace(stokLokasiId);
        Guard.Against.NullOrWhiteSpace(legacyStokId);
        Guard.Against.NullOrWhiteSpace(trsReffId);

        return new StockLegacyBindingModel(
            NunaId.New(IdPrefix),
            BindingKindEnum.LokasiStok,
            stokMutasiId ?? string.Empty,
            stokLokasiId,
            legacyBukuId ?? string.Empty,
            legacyStokId,
            trsReffId);
    }

    public static StockLegacyBindingModel Default =>
        new("-", BindingKindEnum.MutasiBuku, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty);

    public static IStockLegacyBindingKey Key(string id) =>
        new StockLegacyBindingModel(id, BindingKindEnum.MutasiBuku, string.Empty,
            string.Empty, string.Empty, string.Empty, string.Empty);

    #endregion

    #region PROPERTIES

    public string BindingId { get; init; }
    public BindingKindEnum BindingKind { get; init; }
    public string StokMutasiId { get; init; }
    public string StokLokasiId { get; init; }
    public string LegacyBukuId { get; init; }
    public string LegacyStokId { get; init; }
    public string TrsReffId { get; init; }

    #endregion
}
