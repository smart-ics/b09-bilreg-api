namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record BookingCreateCmd(string PasienName, string TglLahir, string Gender,
    string LayananId, string DokterId, string Tgl)
{
    
}