namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record IdentityType
{
    public IdentityType(string jenisId, string nomorId, string nomorKk)
    {
        if (jenisId == string.Empty ^ nomorId == string.Empty)
            throw new ArgumentException("Invalid Identity");
        if (!IsValidJenisId(jenisId))
            throw new ArgumentException("Jenis ID invalid");
        JenisId = jenisId;
        NomorId = nomorId;
        NomorKk = nomorKk;
    }
    public string JenisId { get; init; }
    public string NomorId { get; init; }
    public string NomorKk { get; init; }
    
    public static IdentityType Default => new IdentityType(string.Empty, string.Empty, string.Empty);
    
    //  valid Jenis ID are: KTP, VISA, PASPOR, KITAS, SIM
    private bool IsValidJenisId(string jenisId) 
        => jenisId is "KTP" or "VISA" or "PASPOR" or "KITAS" or "SIM" or "";
}