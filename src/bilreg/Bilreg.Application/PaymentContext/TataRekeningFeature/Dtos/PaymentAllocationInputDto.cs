namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record PaymentAllocationInputDto(
    string PaymentId,
    string PaymentName,
    bool IsTipeJaminan,
    decimal NilaiJasa,
    decimal NilaiObat,
    string CoaId,
    string CoaName);
