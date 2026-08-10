using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public class StockMovementModel : IStokMutasiKey
{
    private const string IdPrefix = "STM";

    #region CREATION

    public StockMovementModel(
        string stokMutasiId,
        string stokLokasiId,
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        string trsReffId,
        MovementKindEnum movementKind,
        decimal qtyIn,
        decimal qtyOut,
        decimal hpp,
        string poReffId,
        DateTime tglMutasi,
        string reversesMutasiId)
    {
        GuardDirection(qtyIn, qtyOut);

        StokMutasiId = stokMutasiId;
        StokLokasiId = stokLokasiId;
        StokBatchId = stokBatchId;
        BrgId = brgId;
        BrgMasukReffId = brgMasukReffId;
        LayananId = layananId;
        TglEd = tglEd;
        TrsReffId = trsReffId;
        MovementKind = movementKind;
        QtyIn = qtyIn;
        QtyOut = qtyOut;
        Hpp = hpp;
        PoReffId = poReffId;
        TglMutasi = tglMutasi;
        ReversesMutasiId = reversesMutasiId;
    }

    public static StockMovementModel CreateInbound(
        string stokLokasiId,
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        string trsReffId,
        MovementKindEnum movementKind,
        decimal qtyIn,
        decimal hpp,
        DateTime tglMutasi,
        string? poReffId = null,
        string? reversesMutasiId = null)
    {
        Guard.Against.NullOrWhiteSpace(stokLokasiId);
        Guard.Against.NullOrWhiteSpace(stokBatchId);
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(brgMasukReffId);
        Guard.Against.NullOrWhiteSpace(layananId);
        Guard.Against.NullOrWhiteSpace(trsReffId);
        if (qtyIn <= 0)
            throw new ArgumentOutOfRangeException(nameof(qtyIn), "Inbound QtyIn must be positive (BR-STL-017).");

        return new StockMovementModel(
            NunaId.New(IdPrefix),
            stokLokasiId,
            stokBatchId,
            brgId,
            brgMasukReffId,
            layananId,
            tglEd,
            trsReffId,
            movementKind,
            qtyIn,
            qtyOut: 0,
            hpp,
            poReffId ?? string.Empty,
            tglMutasi,
            reversesMutasiId ?? string.Empty);
    }

    public static StockMovementModel CreateOutbound(
        string stokLokasiId,
        string stokBatchId,
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        string trsReffId,
        MovementKindEnum movementKind,
        decimal qtyOut,
        decimal hpp,
        DateTime tglMutasi,
        string? poReffId = null,
        string? reversesMutasiId = null)
    {
        Guard.Against.NullOrWhiteSpace(stokLokasiId);
        Guard.Against.NullOrWhiteSpace(stokBatchId);
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(brgMasukReffId);
        Guard.Against.NullOrWhiteSpace(layananId);
        Guard.Against.NullOrWhiteSpace(trsReffId);
        if (qtyOut <= 0)
            throw new ArgumentOutOfRangeException(nameof(qtyOut), "Outbound QtyOut must be positive (BR-STL-017).");

        return new StockMovementModel(
            NunaId.New(IdPrefix),
            stokLokasiId,
            stokBatchId,
            brgId,
            brgMasukReffId,
            layananId,
            tglEd,
            trsReffId,
            movementKind,
            qtyIn: 0,
            qtyOut,
            hpp,
            poReffId ?? string.Empty,
            tglMutasi,
            reversesMutasiId ?? string.Empty);
    }

    public static StockMovementModel Default =>
        new("-", "-", "-", "-", "-", "-", StockLedgerSentinel.EmptyDate, "-",
            MovementKindEnum.GoodsReceipt, 1, 0, 0, string.Empty, StockLedgerSentinel.EmptyDate, string.Empty);

    public static IStokMutasiKey Key(string id) =>
        new StockMovementModel(id, "-", "-", "-", "-", "-", StockLedgerSentinel.EmptyDate, "-",
            MovementKindEnum.GoodsReceipt, 1, 0, 0, string.Empty, StockLedgerSentinel.EmptyDate, string.Empty);

    private static void GuardDirection(decimal qtyIn, decimal qtyOut)
    {
        if (qtyIn > 0 && qtyOut > 0)
            throw new ArgumentException("Stock Movement must not have both QtyIn and QtyOut greater than zero (BR-STL-017).");
        if (qtyIn <= 0 && qtyOut <= 0)
            throw new ArgumentException("Stock Movement must have exactly one of QtyIn or QtyOut greater than zero (BR-STL-017).");
    }

    #endregion

    #region PROPERTIES

    public string StokMutasiId { get; init; }
    public string StokLokasiId { get; init; }
    public string StokBatchId { get; init; }
    public string BrgId { get; init; }
    public string BrgMasukReffId { get; init; }
    public string LayananId { get; init; }
    public DateTime TglEd { get; init; }
    public string TrsReffId { get; init; }
    public MovementKindEnum MovementKind { get; init; }
    public decimal QtyIn { get; init; }
    public decimal QtyOut { get; init; }
    public decimal Hpp { get; init; }
    public string PoReffId { get; init; }
    public DateTime TglMutasi { get; init; }
    public string ReversesMutasiId { get; init; }

    #endregion
}
