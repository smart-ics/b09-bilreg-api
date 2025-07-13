namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienViewType(
    string PasienId,
    string NomorMedRec,
    string PasienName,
    DateTime TglLahir,    
    GenderType Gender);