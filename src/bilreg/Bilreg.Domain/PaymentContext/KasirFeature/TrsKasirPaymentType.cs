using Bilreg.Domain.Shared;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public record TrsKasirPaymentType
{
    public TrsKasirPaymentType(
        string paymentRowId,
        string trsKasirId,
        int noUrut,
        string metodeBayar,
        decimal nilaiBayar,
        decimal nilaiKembalian = 0,
        string nomorKartu = "",
        string nomorReferensi = "",
        string namaBank = "",
        string kodeJenisPayment = "",
        string namaMetodeBayar = "",
        string coaId = "")
    {
        TrsKasirId = trsKasirId?.Trim() ?? string.Empty;
        NoUrut = noUrut;
        PaymentRowId = string.IsNullOrWhiteSpace(paymentRowId)
            ? (!string.IsNullOrWhiteSpace(TrsKasirId) ? $"{TrsKasirId}-{NoUrut:D2}" : $"{NoUrut:D2}")
            : paymentRowId.Trim();
        MetodeBayar = (metodeBayar ?? string.Empty).Trim().ToUpperInvariant();
        NilaiBayar = nilaiBayar;
        NilaiKembalian = nilaiKembalian;
        NomorKartu = (nomorKartu ?? string.Empty).Trim();
        NomorReferensi = (nomorReferensi ?? string.Empty).Trim();
        NamaBank = (namaBank ?? string.Empty).Trim();
        KodeJenisPayment = (kodeJenisPayment ?? string.Empty).Trim();
        NamaMetodeBayar = (namaMetodeBayar ?? string.Empty).Trim();
        CoaId = (coaId ?? string.Empty).Trim();

        Validate();
    }

    public string PaymentRowId { get; init; }
    public string TrsKasirId { get; init; }
    public int NoUrut { get; init; }
    public string MetodeBayar { get; init; }
    public decimal NilaiBayar { get; init; }
    public decimal NilaiKembalian { get; init; }
    public string NomorKartu { get; init; }
    public string NomorReferensi { get; init; }
    public string NamaBank { get; init; }
    public string KodeJenisPayment { get; init; }
    public string NamaMetodeBayar { get; init; }
    public string CoaId { get; init; }

    public decimal NilaiNetto => NilaiBayar - NilaiKembalian;

    public static TrsKasirPaymentType Create(
        string metodeBayar,
        decimal nilaiBayar,
        decimal nilaiKembalian = 0,
        string nomorKartu = "",
        string nomorReferensi = "",
        string namaBank = "",
        int noUrut = 1,
        string trsKasirId = "",
        string kodeJenisPayment = "",
        string namaMetodeBayar = "",
        string coaId = "")
    {
        var rowId = !string.IsNullOrWhiteSpace(trsKasirId)
            ? $"{trsKasirId.Trim()}-{noUrut:D2}"
            : $"{noUrut:D2}";

        return new TrsKasirPaymentType(
            rowId,
            trsKasirId,
            noUrut,
            metodeBayar,
            nilaiBayar,
            nilaiKembalian,
            nomorKartu,
            nomorReferensi,
            namaBank,
            kodeJenisPayment,
            namaMetodeBayar,
            coaId);
    }

    public TrsKasirPaymentType WithTransaction(string trsKasirId, int noUrut)
    {
        var cleanTrsId = trsKasirId?.Trim() ?? string.Empty;
        var rowId = !string.IsNullOrWhiteSpace(cleanTrsId)
            ? $"{cleanTrsId}-{noUrut:D2}"
            : $"{noUrut:D2}";

        return new TrsKasirPaymentType(
            rowId,
            cleanTrsId,
            noUrut,
            MetodeBayar,
            NilaiBayar,
            NilaiKembalian,
            NomorKartu,
            NomorReferensi,
            NamaBank,
            KodeJenisPayment,
            NamaMetodeBayar,
            CoaId);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(MetodeBayar))
            throw new BusinessValidationException("Metode bayar wajib diisi");

        if (!KasirConst.AllowedMetodeBayar.Contains(MetodeBayar))
            throw new BusinessValidationException($"Metode bayar '{MetodeBayar}' tidak valid");

        if (NilaiBayar < 0)
            throw new BusinessValidationException("Nilai bayar tidak boleh negatif");

        if (NilaiKembalian < 0)
            throw new BusinessValidationException("Nilai kembalian tidak boleh negatif");

        if (MetodeBayar != KasirConst.MetodeBayarTunai)
        {
            if (NilaiKembalian > 0)
                throw new BusinessValidationException("Nilai kembalian hanya diizinkan untuk metode pembayaran TUNAI");
        }
        else
        {
            if (NilaiKembalian > NilaiBayar)
                throw new BusinessValidationException("Nilai kembalian tidak boleh melebihi nilai bayar tunai");
        }
    }
}
