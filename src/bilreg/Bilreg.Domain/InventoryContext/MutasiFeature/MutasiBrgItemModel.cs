using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class MutasiBrgItemModel
{
    #region CREATION
    public MutasiBrgItemModel(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        SatuanType satuan)
    {
        Guard.Against.Null(brg, nameof(brg));
        if (string.IsNullOrWhiteSpace(brg.BrgId) || brg.BrgId == "-")
            throw new ArgumentException("Barang invalid", nameof(brg));

        Guard.Against.Null(satuan, nameof(satuan));
        if (string.IsNullOrWhiteSpace(satuan.SatuanId) || satuan.SatuanId == "-")
            throw new ArgumentException("Satuan invalid", nameof(satuan));

        if (qtyKirim <= 0)
            throw new ArgumentException("QtyKirim harus lebih dari 0", nameof(qtyKirim));

        NoUrut = noUrut;
        Brg = brg;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        QtyKirim = qtyKirim;
        Satuan = satuan;
    }

    public static MutasiBrgItemModel Create(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        SatuanType satuan)
        => new(noUrut, brg, batchNo, expiredDate, qtyKirim, satuan);

    public static MutasiBrgItemModel Create(
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        SatuanType satuan)
        => new(0, brg, batchNo, expiredDate, qtyKirim, satuan);

    public static MutasiBrgItemModel Create(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        SatuanReff satuan)
    {
        Guard.Against.Null(satuan, nameof(satuan));
        return new(noUrut, brg, batchNo, expiredDate, qtyKirim, new SatuanType(satuan.SatuanId, satuan.SatuanName));
    }

    public static MutasiBrgItemModel Default
        => new(0, new BrgReff("DEFAULT", "DEFAULT"), string.Empty, string.Empty, 1, new SatuanType("DEFAULT", "DEFAULT"));
    #endregion

    #region PROPERTIES
    public int NoUrut { get; internal set; }
    public BrgReff Brg { get; private set; }
    public string BatchNo { get; private set; }
    public string ExpiredDate { get; private set; }
    public decimal QtyKirim { get; private set; }
    public SatuanType Satuan { get; private set; }
    #endregion

    #region BEHAVIOR
    public void SetNoUrut(int noUrut)
    {
        if (noUrut <= 0)
            throw new ArgumentException("NoUrut invalid", nameof(noUrut));

        NoUrut = noUrut;
    }

    public void SetQtyKirim(decimal qtyKirim)
    {
        if (qtyKirim <= 0)
            throw new ArgumentException("QtyKirim harus lebih dari 0", nameof(qtyKirim));

        QtyKirim = qtyKirim;
    }

    public void SetBatch(string batchNo, string expiredDate)
    {
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
    }
    #endregion
}
