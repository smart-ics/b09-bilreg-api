namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record TataRekeningPatientContextDto(
    string PasienId,
    string PasienName,
    string Gender,
    string TglLahir,
    string LayananName,
    string JenisReg,
    string TipeJaminanId,
    string TipeJaminanName,
    string? KelasName,
    string? DpjpName,
    decimal AdvancePayment,
    decimal DepositPayment);
