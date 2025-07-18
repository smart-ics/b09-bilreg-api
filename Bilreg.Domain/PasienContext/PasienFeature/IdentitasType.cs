using Ardalis.GuardClauses;
using Bilreg.Domain.Helpers;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public record IdentitasType
{
    private static readonly string[] AllowedJenisId = ["KTP", "VISA", "PASPOR", "KITAS", "SIM", "KK"];
    public IdentitasType(string jenisId, string nomorId)
    {
        JenisId = jenisId;
        NomorId = nomorId;
    }
    
    public string JenisId { get; init; }
    public string NomorId { get; init; }

    public static IdentitasType CreateNew(string jenisId, string nomorId)
    {
        Guard.Against.NotInAllowedValues(jenisId, AllowedJenisId, nameof(jenisId));
        return new IdentitasType(jenisId, nomorId);
    }
    public static IdentitasType Default => new IdentitasType("-", "-");
    public static IdentitasType Ktp => new IdentitasType("KTP", "-");
    public static IdentitasType Visa => new IdentitasType("VISA", "-");
    public static IdentitasType Paspor => new IdentitasType("PASPOR", "-");
    public static IdentitasType Kitas => new IdentitasType("KITAS", "-");
    public static IdentitasType Sim => new IdentitasType("SIM", "-");
    public static IdentitasType Kk => new IdentitasType("KK", "-");
}