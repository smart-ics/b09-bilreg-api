namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public record TataRekeningQueueDto(
    string RegId,
    DateTime RegDate,
    string PasienId,
    string JenisReg,
    string LayananId,
    string DokterId,
    string TipeJaminanId,
    string PasienName,
    string TglLahir,
    string Gender,
    string LayananName,
    string DokterName,
    string TipeJaminanName,
    int StatusTataRekening);
