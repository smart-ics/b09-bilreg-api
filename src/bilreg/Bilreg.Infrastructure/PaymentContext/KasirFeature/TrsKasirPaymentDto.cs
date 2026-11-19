using Bilreg.Domain.PaymentContext.KasirFeature;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public record TrsKasirPaymentDto(
    string PaymentRowId,
    string TrsKasirId,
    int NoUrut,
    string MetodeBayar,
    decimal NilaiBayar,
    decimal NilaiKembalian,
    string NomorKartu,
    string NomorReferensi,
    string NamaBank,
    string KodeJenisPayment = "",
    string NamaMetodeBayar = "",
    string CoaId = "",
    decimal NilaiNetto = 0)
{
    public static TrsKasirPaymentDto FromModel(TrsKasirPaymentType model, string trsKasirId = "")
    {
        return new TrsKasirPaymentDto(
            PaymentRowId: model.PaymentRowId,
            TrsKasirId: string.IsNullOrWhiteSpace(model.TrsKasirId) ? trsKasirId : model.TrsKasirId,
            NoUrut: model.NoUrut,
            MetodeBayar: model.MetodeBayar,
            NilaiBayar: model.NilaiBayar,
            NilaiKembalian: model.NilaiKembalian,
            NomorKartu: model.NomorKartu ?? string.Empty,
            NomorReferensi: model.NomorReferensi ?? string.Empty,
            NamaBank: model.NamaBank ?? string.Empty,
            KodeJenisPayment: model.KodeJenisPayment ?? string.Empty,
            NamaMetodeBayar: model.NamaMetodeBayar ?? string.Empty,
            CoaId: model.CoaId ?? string.Empty,
            NilaiNetto: model.NilaiNetto);
    }

    public TrsKasirPaymentType ToModel()
    {
        return new TrsKasirPaymentType(
            paymentRowId: PaymentRowId,
            trsKasirId: TrsKasirId,
            noUrut: NoUrut,
            metodeBayar: MetodeBayar,
            nilaiBayar: NilaiBayar,
            nilaiKembalian: NilaiKembalian,
            nomorKartu: NomorKartu ?? string.Empty,
            nomorReferensi: NomorReferensi ?? string.Empty,
            namaBank: NamaBank ?? string.Empty,
            kodeJenisPayment: KodeJenisPayment ?? string.Empty,
            namaMetodeBayar: NamaMetodeBayar ?? string.Empty,
            coaId: CoaId ?? string.Empty);
    }
}
