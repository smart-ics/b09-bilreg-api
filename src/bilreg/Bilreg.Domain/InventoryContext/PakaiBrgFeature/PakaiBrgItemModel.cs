using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.InventoryContext.PakaiBrgFeature;

public class PakaiBrgItemModel
{
    private const int MAX_BATCH_LENGTH = 15;
    private const int MAX_KETERANGAN_LENGTH = 30;

    #region CREATION
    public PakaiBrgItemModel(
        int noUrut,
        BrgReff brg,
        SatuanReff satuan,
        decimal stokCurrent,
        decimal qty,
        string noBatch,
        DateTime tglEd,
        decimal hpp,
        decimal subTotal,
        string keterangan,
        bool isVoid)
    {
        Guard.Against.Null(brg, nameof(brg));
        Guard.Against.NullOrWhiteSpace(brg.BrgId, nameof(brg.BrgId));
        Guard.Against.Null(satuan, nameof(satuan));

        if (qty <= 0)
            throw new ArgumentException("Qty pemakaian harus lebih besar dari 0.", nameof(qty));

        if (hpp < 0)
            throw new ArgumentException("HPP tidak boleh negatif.", nameof(hpp));

        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        StokCurrent = stokCurrent;
        Qty = qty;
        NoBatch = TruncateSafe(noBatch, MAX_BATCH_LENGTH);
        TglEd = tglEd;
        Hpp = hpp;
        SubTotal = subTotal;
        Keterangan = TruncateSafe(keterangan, MAX_KETERANGAN_LENGTH);
        IsVoid = isVoid;
    }

    public static PakaiBrgItemModel Create(
        int noUrut,
        BrgReff brg,
        SatuanReff satuan,
        decimal stokCurrent,
        decimal qty,
        string noBatch,
        DateTime tglEd,
        decimal hpp,
        string keterangan)
    {
        var subTotal = qty * hpp;
        return new PakaiBrgItemModel(
            noUrut,
            brg,
            satuan,
            stokCurrent,
            qty,
            noBatch,
            tglEd,
            hpp,
            subTotal,
            keterangan,
            isVoid: false);
    }

    public static PakaiBrgItemModel Rehydrate(
        int noUrut,
        BrgReff brg,
        SatuanReff satuan,
        decimal stokCurrent,
        decimal qty,
        string noBatch,
        DateTime tglEd,
        decimal hpp,
        decimal subTotal,
        string keterangan,
        bool isVoid)
    {
        return new PakaiBrgItemModel(
            noUrut,
            brg,
            satuan,
            stokCurrent,
            qty,
            noBatch,
            tglEd,
            hpp,
            subTotal,
            keterangan,
            isVoid);
    }
    #endregion

    #region PROPERTIES
    public int NoUrut { get; internal set; }
    public BrgReff Brg { get; private set; }
    public SatuanReff Satuan { get; private set; }
    public decimal StokCurrent { get; private set; }
    public decimal Qty { get; private set; }
    public string NoBatch { get; private set; }
    public DateTime TglEd { get; private set; }
    public decimal Hpp { get; private set; }
    public decimal SubTotal { get; private set; }
    public string Keterangan { get; private set; }
    public bool IsVoid { get; private set; }
    #endregion

    #region BEHAVIOR
    internal void SetNoUrut(int noUrut)
    {
        if (noUrut <= 0)
            throw new ArgumentException("NoUrut harus lebih besar dari 0.", nameof(noUrut));

        NoUrut = noUrut;
    }

    internal void Void()
    {
        IsVoid = true;
    }

    private static string TruncateSafe(string? value, int maxLength)
    {
        var safe = (value ?? string.Empty).Trim();
        return safe.Length > maxLength ? safe[..maxLength] : safe;
    }
    #endregion
}
