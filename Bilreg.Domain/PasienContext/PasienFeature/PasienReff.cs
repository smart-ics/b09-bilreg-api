namespace Bilreg.Domain.PasienContext.PasienFeature;

public record PasienReff(
    string PasienId,
    string PasienName,
    DateTime TglLahir,    
    string Gender);