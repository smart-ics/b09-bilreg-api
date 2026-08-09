using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public class LocationStockBalanceModel : IStokLokasiKey
{
    private const string IdPrefix = "STL";

    #region CREATION

    public LocationStockBalanceModel(
        string stokLokasiId,
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        string noBatch,
        DateTime tglMasuk,
        decimal qtySisa,
        long version)
    {
        StokLokasiId = stokLokasiId;
        StokBatchId = stokBatchId;
        BrgId = brgId;
        BrgMasukReffId = brgMasukReffId;
        LayananId = layananId;
        TglEd = tglEd;
        NoBatch = noBatch;
        TglMasuk = tglMasuk;
        QtySisa = qtySisa;
        Version = version;
    }

    public static LocationStockBalanceModel Create(
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        DateTime tglMasuk,
        decimal qtySisa,
        string? noBatch = null)
    {
        Guard.Against.NullOrWhiteSpace(stokBatchId);
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(brgMasukReffId);
        Guard.Against.NullOrWhiteSpace(layananId);
        if (qtySisa < 0)
            throw new ArgumentOutOfRangeException(nameof(qtySisa), "Remaining Quantity shall not become negative (BR-STL-010).");

        return new LocationStockBalanceModel(
            NunaId.New(IdPrefix),
            stokBatchId,
            brgId,
            brgMasukReffId,
            layananId,
            tglEd,
            noBatch ?? string.Empty,
            tglMasuk,
            qtySisa,
            version: 0);
    }

    public static LocationStockBalanceModel Default =>
        new("-", "-", "-", "-", "-", StockLedgerSentinel.EmptyDate, string.Empty,
            StockLedgerSentinel.EmptyDate, 0, 0);

    public static IStokLokasiKey Key(string id) =>
        new LocationStockBalanceModel(id, "-", "-", "-", "-", StockLedgerSentinel.EmptyDate,
            string.Empty, StockLedgerSentinel.EmptyDate, 0, 0);

    #endregion

    #region PROPERTIES

    public string StokLokasiId { get; init; }
    public string StokBatchId { get; init; }
    public string BrgId { get; init; }
    public string BrgMasukReffId { get; init; }
    public string LayananId { get; init; }
    public DateTime TglEd { get; init; }
    /// <summary>Optional manufacturer batch; stored but excluded from L1 uniqueness (GAP-STL-005).</summary>
    public string NoBatch { get; private set; }
    public DateTime TglMasuk { get; init; }
    public decimal QtySisa { get; private set; }
    public long Version { get; private set; }

    #endregion

    #region BEHAVIOUR

    public void IncreaseQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Increase quantity must be positive.");

        QtySisa += qty;
        Version++;
    }

    public void DecreaseQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Decrease quantity must be positive.");
        if (QtySisa - qty < 0)
            throw new InvalidOperationException("Remaining Quantity shall not become negative (BR-STL-010).");

        QtySisa -= qty;
        Version++;
        // QtySisa == 0 retained as Depleted Balance (BR-STL-011, BR-STL-012).
    }

    #endregion
}
