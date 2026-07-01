using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaContactHidokDto(string DokterRs, string DokterHidok)
{
    public static PpaContactHidokDto FromModel(string ppaId, ContactType model)
    {
        return new(ppaId, model.ContactDetail);
    }

    public ContactType ToModel()
    {
        return new(JenisContactEnum.Email, DokterHidok);
    }
}