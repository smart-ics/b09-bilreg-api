namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record ContactType(string Email, string NoTelp, string NoHp)
{
    public static ContactType Default => new ContactType(string.Empty, string.Empty, string.Empty);
}