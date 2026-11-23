using Bilreg.Domain.PaymentContext.KasirFeature;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsKasirPaymentDto
{
    public string PaymentRowId { get; set; } = string.Empty;
    public string TrsKasirId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string MetodeBayar { get; set; } = string.Empty;
    public string KodeJenisPayment { get; set; } = string.Empty;
    public string NamaMetodeBayar { get; set; } = string.Empty;
    public string CoaId { get; set; } = string.Empty;
    public decimal NilaiBayar { get; set; }
    public decimal NilaiKembalian { get; set; }
    public decimal NilaiNetto { get; set; }
    public string NomorKartu { get; set; } = string.Empty;
    public string NomorReferensi { get; set; } = string.Empty;
    public string NamaBank { get; set; } = string.Empty;

    public TrsKasirPaymentDto()
    {
    }

    public TrsKasirPaymentDto(
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
        this.PaymentRowId = PaymentRowId;
        this.TrsKasirId = TrsKasirId;
        this.NoUrut = NoUrut;
        this.MetodeBayar = MetodeBayar;
        this.NilaiBayar = NilaiBayar;
        this.NilaiKembalian = NilaiKembalian;
        this.NomorKartu = NomorKartu ?? string.Empty;
        this.NomorReferensi = NomorReferensi ?? string.Empty;
        this.NamaBank = NamaBank ?? string.Empty;
        this.KodeJenisPayment = KodeJenisPayment ?? string.Empty;
        this.NamaMetodeBayar = NamaMetodeBayar ?? string.Empty;
        this.CoaId = CoaId ?? string.Empty;
        this.NilaiNetto = NilaiNetto;
    }

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
