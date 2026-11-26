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
        decimal qtyKirim,
        decimal qtyTerimaBaik,
        decimal qtySelisih,
        SatuanType satuan,
        string alasanSelisih = "")
    {
        Guard.Against.Null(brg, nameof(brg));
        if (string.IsNullOrWhiteSpace(brg.BrgId) || brg.BrgId == "-")
            throw new ArgumentException("Barang invalid", nameof(brg));

        Guard.Against.Null(satuan, nameof(satuan));
        if (string.IsNullOrWhiteSpace(satuan.SatuanId) || satuan.SatuanId == "-")
            throw new ArgumentException("Satuan invalid", nameof(satuan));

        if (qtyKirim <= 0)
            throw new ArgumentException("QtyKirim harus lebih dari 0", nameof(qtyKirim));

        if (qtyTerimaBaik < 0)
            throw new ArgumentException("QtyTerimaBaik tidak boleh negatif", nameof(qtyTerimaBaik));

        if (qtySelisih < 0)
            throw new ArgumentException("QtySelisih tidak boleh negatif", nameof(qtySelisih));

        if (qtyTerimaBaik + qtySelisih != qtyKirim)
            throw new ArgumentException("QtyTerimaBaik + QtySelisih harus sama dengan QtyKirim", nameof(qtyTerimaBaik));

        NoUrut = noUrut;
        Brg = brg;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        QtyKirim = qtyKirim;
        QtyTerimaBaik = qtyTerimaBaik;
        QtySelisih = qtySelisih;
        Satuan = satuan;
        AlasanSelisih = alasanSelisih?.Trim() ?? string.Empty;
    }

    public static TerimaMutasiItemModel CreateConfirmed(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        decimal qtyTerimaBaik,
        decimal qtySelisih,
        SatuanType satuan,
        string alasanSelisih = "")
    {
        if (qtySelisih > 0 && string.IsNullOrWhiteSpace(alasanSelisih))
            throw new ArgumentException("AlasanSelisih wajib diisi jika QtySelisih > 0", nameof(alasanSelisih));

        return new TerimaMutasiItemModel(
            noUrut,
            brg,
            batchNo,
            expiredDate,
            qtyKirim,
            qtyTerimaBaik,
            qtySelisih,
            satuan,
            alasanSelisih);
    }

    public static TerimaMutasiItemModel CreateRejected(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        SatuanType satuan,
        string alasanSelisih = "")
    {
        return new TerimaMutasiItemModel(
            noUrut,
            brg,
            batchNo,
            expiredDate,
            qtyKirim,
            qtyTerimaBaik: 0m,
            qtySelisih: qtyKirim,
            satuan,
            alasanSelisih);
    }

    public static TerimaMutasiItemModel Load(
        int noUrut,
        BrgReff brg,
        string batchNo,
        string expiredDate,
        decimal qtyKirim,
        decimal qtyTerimaBaik,
        decimal qtySelisih,
        SatuanType satuan,
        string alasanSelisih = "")
        => new(noUrut, brg, batchNo, expiredDate, qtyKirim, qtyTerimaBaik, qtySelisih, satuan, alasanSelisih);

    public static TerimaMutasiItemModel Default
        => new(0, new BrgReff("DEFAULT", "DEFAULT"), string.Empty, string.Empty, 1m, 1m, 0m, new SatuanType("DEFAULT", "DEFAULT"));
    #endregion

    #region PROPERTIES
    public int NoUrut { get; init; }
    public BrgReff Brg { get; init; }
    public string BatchNo { get; init; }
    public string ExpiredDate { get; init; }
    public decimal QtyKirim { get; init; }
    public decimal QtyTerimaBaik { get; init; }
    public decimal QtySelisih { get; init; }
    public SatuanType Satuan { get; init; }
    public string AlasanSelisih { get; init; }
    #endregion

    internal TerimaMutasiItemModel WithNoUrut(int noUrut)
        => new(noUrut, Brg, BatchNo, ExpiredDate, QtyKirim, QtyTerimaBaik, QtySelisih, Satuan, AlasanSelisih);
}
