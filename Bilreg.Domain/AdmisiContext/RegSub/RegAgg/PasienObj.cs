namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record PasienObj(
    string PasienId,
    string NomorMedrec,
    string PasienName,
    DateTime TglLahir,
    string Gender);