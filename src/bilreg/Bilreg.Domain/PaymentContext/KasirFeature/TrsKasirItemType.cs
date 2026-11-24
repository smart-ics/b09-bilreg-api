using Bilreg.Domain.Shared;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface ITrsKasirItemKey
{
    string KasirItemId { get; }
}

public record TrsKasirItemKey(string KasirItemId) : ITrsKasirItemKey;

public record TrsKasirItemType : ITrsKasirItemKey
{
    public TrsKasirItemType(
        string kasirItemId,
        string trsKasirId,
        int noUrut,
        string billId,
        string regId,
        string layananId,
        string keterangan,
        decimal nilaiBill,
        decimal nilaiBayar)
    {
        TrsKasirId = trsKasirId?.Trim() ?? string.Empty;
        NoUrut = noUrut;
        KasirItemId = string.IsNullOrWhiteSpace(kasirItemId)
            ? (!string.IsNullOrWhiteSpace(TrsKasirId) ? $"{TrsKasirId}-{NoUrut:D2}" : $"{NoUrut:D2}")
            : kasirItemId.Trim();
        BillId = billId?.Trim() ?? string.Empty;
        RegId = regId?.Trim() ?? string.Empty;
        LayananId = layananId?.Trim() ?? string.Empty;
        Keterangan = keterangan?.Trim() ?? string.Empty;
        NilaiBill = nilaiBill;
        NilaiBayar = nilaiBayar;

        Validate();
    }

    public string KasirItemId { get; init; }
    public string TrsKasirId { get; init; }
    public int NoUrut { get; init; }
    public string BillId { get; init; }
    public string RegId { get; init; }
    public string LayananId { get; init; }
    public string Keterangan { get; init; }
    public decimal NilaiBill { get; init; }
    public decimal NilaiBayar { get; init; }

    public static TrsKasirItemType Create(
        string billId,
        string regId,
        string layananId,
        string keterangan,
        decimal nilaiBill,
        decimal nilaiBayar,
        int noUrut = 1,
        string trsKasirId = "")
    {
        var kasirItemId = !string.IsNullOrWhiteSpace(trsKasirId)
            ? $"{trsKasirId.Trim()}-{noUrut:D2}"
            : $"{noUrut:D2}";

        return new TrsKasirItemType(
            kasirItemId,
            trsKasirId,
            noUrut,
            billId,
            regId,
            layananId,
            keterangan,
            nilaiBill,
            nilaiBayar);
    }

    public TrsKasirItemType WithTransaction(string trsKasirId, int noUrut)
    {
        var cleanTrsId = trsKasirId?.Trim() ?? string.Empty;
        var kasirItemId = !string.IsNullOrWhiteSpace(cleanTrsId)
            ? $"{cleanTrsId}-{noUrut:D2}"
            : $"{noUrut:D2}";

        return new TrsKasirItemType(
            kasirItemId,
            cleanTrsId,
            noUrut,
            BillId,
            RegId,
            LayananId,
            Keterangan,
            NilaiBill,
            NilaiBayar);
    }

    private void Validate()
    {
        if (NoUrut < 1)
            throw new BusinessValidationException("Nomor urut item kasir harus lebih besar dari nol");

        if (string.IsNullOrWhiteSpace(BillId))
            throw new BusinessValidationException("BillId wajib diisi");

        if (NilaiBill < 0)
            throw new BusinessValidationException("Nilai bill tidak boleh negatif");

        if (NilaiBayar < 0)
            throw new BusinessValidationException("Nilai bayar item tidak boleh negatif");

        if (NilaiBayar > NilaiBill)
            throw new BusinessValidationException("Nilai bayar item tidak boleh melebihi nilai bill");
    }
}
