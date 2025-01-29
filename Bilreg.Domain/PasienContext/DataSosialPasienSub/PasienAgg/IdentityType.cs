namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record IdentityType(string JenisId, string NomorId, string NomorKk)
{
    public static IdentityType Default => new IdentityType(string.Empty, string.Empty, string.Empty);   
}