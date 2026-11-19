namespace Bilreg.Application.PaymentContext.KasirFeature;

public record PaymentItemInput(
    string MetodeBayar,
    decimal NilaiBayar,
    decimal NilaiKembalian = 0,
    string NomorKartu = "",
    string NomorReferensi = "",
    string NamaBank = "",
    string KodeJenisPayment = "",
    string NamaMetodeBayar = "",
    string CoaId = "");

public record PaymentItemResponse(
    string PaymentRowId,
    int NoUrut,
    string MetodeBayar,
    decimal NilaiBayar,
    decimal NilaiKembalian,
    decimal NilaiNetto,
    string NomorKartu,
    string NomorReferensi,
    string NamaBank,
    string KodeJenisPayment = "",
    string NamaMetodeBayar = "",
    string CoaId = "");
