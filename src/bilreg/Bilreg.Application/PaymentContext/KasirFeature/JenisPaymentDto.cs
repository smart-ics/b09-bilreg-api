namespace Bilreg.Application.PaymentContext.KasirFeature;

public record JenisPaymentDto(
    string KodeJenisPayment,
    string NamaJenisPayment,
    string CoaId,
    bool IsKredit,
    string Kategori);

public record JenisPaymentGroupDto(
    string Kategori,
    IEnumerable<JenisPaymentDto> Items);
