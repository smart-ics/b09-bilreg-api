namespace Bilreg.Domain.PasienContext.PasienFeature;

public record PasienReff(
    string PasienId,
    string PasienName,
    DateOnly TglLahir,    
    string Gender);