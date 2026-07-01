namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record PaymentProjectionDto(
    string PaymentId,
    string PaymentName,
    bool IsTipeJaminan,
    decimal NilaiJasa,
    decimal NilaiObat);
