namespace Bilreg.Domain.PaymentContext.KasirFeature;

public static class KasirConst
{
    public const string JenisTrsTerima = "TERIMA";
    public const string JenisTrsKeluar = "KELUAR";

    public const string StatusTrsBerhasil = "BERHASIL";
    public const string StatusTrsVoid = "VOID";

    public const string StatusShiftBuka = "BUKA";
    public const string StatusShiftTutup = "TUTUP";

    public const string MetodeBayarTunai = "TUNAI";
    public const string MetodeBayarDebit = "DEBIT";
    public const string MetodeBayarKredit = "KREDIT";
    public const string MetodeBayarTransfer = "TRANSFER";
    public const string MetodeBayarQris = "QRIS";

    public static readonly string[] AllowedMetodeBayar =
    [
        MetodeBayarTunai,
        MetodeBayarDebit,
        MetodeBayarKredit,
        MetodeBayarTransfer,
        MetodeBayarQris
    ];
}
