using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class TerimaMutasiItemModel
{
    #region CREATION
    public TerimaMutasiItemModel(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyTerima,
        SatuanType satuan)
    {
        Guard.Against.Null(brg, nameof(brg));
        if (string.IsNullOrWhiteSpace(brg.BrgId) || brg.BrgId == "-")
            throw new ArgumentException("Barang invalid", nameof(brg));

        Guard.Against.Null(satuan, nameof(satuan));
        if (string.IsNullOrWhiteSpace(satuan.SatuanId) || satuan.SatuanId == "-")
            throw new ArgumentException("Satuan invalid", nameof(satuan));

        if (qtyTerima <= 0)
            throw new ArgumentException("QtyTerima harus lebih dari 0", nameof(qtyTerima));

        NoUrut = noUrut;
        Brg = brg;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        QtyTerima = qtyTerima;
        Satuan = satuan;
    }

    public static TerimaMutasiItemModel CreateConfirmed(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyTerima,
        SatuanType satuan)
        => new(noUrut, brg, batchNo, expiredDate, qtyTerima, satuan);

    public static TerimaMutasiItemModel Load(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyTerima,
        SatuanType satuan)
        => new(noUrut, brg, batchNo, expiredDate, qtyTerima, satuan);

    public static TerimaMutasiItemModel Default
        => new(0, new BrgReff("DEFAULT", "DEFAULT"), string.Empty, string.Empty, 1m, new SatuanType("DEFAULT", "DEFAULT"));
    #endregion

    #region PROPERTIES
    public int NoUrut { get; init; }
    public BrgReff Brg { get; init; }
    public string BatchNo { get; init; }
    public string ExpiredDate { get; init; }
    public decimal QtyTerima { get; init; }
    public SatuanType Satuan { get; init; }
    #endregion

    internal TerimaMutasiItemModel WithNoUrut(int noUrut)
        => new(noUrut, Brg, BatchNo, ExpiredDate, QtyTerima, Satuan);
}
