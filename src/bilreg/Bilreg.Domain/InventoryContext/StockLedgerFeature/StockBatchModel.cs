using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public class StockBatchModel : IStockBatchKey
{
    private const string IdPrefix = "STB";
    private readonly List<LocationStockBalanceModel> _listLokasi;

    #region CREATION

    public StockBatchModel(
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        decimal qtySisa,
        decimal hpp,
        DateTime tglMasuk,
        string poReffId,
        long version,
        IEnumerable<LocationStockBalanceModel> listLokasi)
    {
        StokBatchId = stokBatchId;
        BrgId = brgId;
        BrgMasukReffId = brgMasukReffId;
        QtySisa = qtySisa;
        Hpp = hpp;
        TglMasuk = tglMasuk;
        PoReffId = poReffId;
        Version = version;
        PersistedVersion = version;
        _listLokasi = listLokasi?.ToList() ?? [];
    }

    public static StockBatchModel Default =>
        new("-", "-", "-", 0, 0, StockLedgerSentinel.EmptyDate, string.Empty, 0, []);

    public static IStockBatchKey Key(string id) =>
        new StockBatchModel(id, "-", "-", 0, 0, StockLedgerSentinel.EmptyDate, string.Empty, 0, []);

    public static StockBatchModel Create(
        string brgId,
        string brgMasukReffId,
        decimal hpp,
        DateTime tglMasuk,
        string? poReffId = null)
    {
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(brgMasukReffId);
        if (hpp < 0)
            throw new ArgumentOutOfRangeException(nameof(hpp), "Hpp must not be negative.");

        return new StockBatchModel(
            NunaId.New(IdPrefix),
            brgId,
            brgMasukReffId,
            qtySisa: 0,
            hpp,
            tglMasuk,
            poReffId ?? string.Empty,
            version: 0,
            listLokasi: []);
    }

    #endregion

    #region PROPERTIES

    public string StokBatchId { get; init; }
    public string BrgId { get; init; }
    public string BrgMasukReffId { get; init; }
    public decimal QtySisa { get; private set; }
    public decimal Hpp { get; init; }
    public DateTime TglMasuk { get; init; }
    public string PoReffId { get; init; }
    public long Version { get; private set; }
    /// <summary>Version last accepted by persistence (load or successful SaveChanges).</summary>
    public long PersistedVersion { get; private set; }

    public IEnumerable<LocationStockBalanceModel> ListLokasi => _listLokasi;

    public decimal LokasiQtyTotal => _listLokasi.Sum(x => x.QtySisa);

    public StockBatchKeyType ToNaturalKey() => new(BrgId, BrgMasukReffId);

    #endregion

    #region BEHAVIOUR

    public LocationStockBalanceModel IncreaseLokasi(
        string layananId,
        DateTime tglEd,
        decimal qty,
        string? noBatch = null)
    {
        Guard.Against.NullOrWhiteSpace(layananId);
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Increase quantity must be positive.");

        var lokasi = FindLokasi(layananId, tglEd);
        if (lokasi is null)
        {
            lokasi = LocationStockBalanceModel.Create(
                StokBatchId, BrgId, BrgMasukReffId, layananId, tglEd, TglMasuk, qty, noBatch);
            _listLokasi.Add(lokasi);
        }
        else
        {
            lokasi.IncreaseQty(qty);
        }

        SyncHospitalQty();
        return lokasi;
    }

    public LocationStockBalanceModel DecreaseLokasi(string layananId, DateTime tglEd, decimal qty)
    {
        Guard.Against.NullOrWhiteSpace(layananId);
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Decrease quantity must be positive.");

        var lokasi = FindLokasi(layananId, tglEd)
            ?? throw new InvalidOperationException(
                $"Location Stock Balance not found for LayananId={layananId}, TglEd={tglEd:yyyy-MM-dd}.");

        lokasi.DecreaseQty(qty);
        // Depleted balance (QtySisa == 0) remains in collection (BR-STL-011, BR-STL-012).
        SyncHospitalQty();
        return lokasi;
    }

    private LocationStockBalanceModel? FindLokasi(string layananId, DateTime tglEd) =>
        _listLokasi.FirstOrDefault(x =>
            x.LayananId == layananId && x.TglEd == tglEd);

    private void SyncHospitalQty()
    {
        QtySisa = LokasiQtyTotal;
        Version++;
    }

    public void AcceptPersisted() => PersistedVersion = Version;

    #endregion
}
