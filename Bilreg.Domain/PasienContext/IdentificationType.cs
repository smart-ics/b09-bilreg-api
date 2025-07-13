using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record IdentificationType
{
    public IdentificationType(string jenisId, string nomorId)
    {
        Guard.Against.NullOrWhiteSpace(jenisId, nameof(jenisId));
        Guard.Against.NullOrWhiteSpace(nomorId, nameof(nomorId));

        JenisId = jenisId;
        NomorId = nomorId;
    }
    public string JenisId { get; init; }
    public string NomorId { get; init; }
    
    public static IdentificationType Default => new IdentificationType("-", "-");
    public static IdentificationType Ktp => new IdentificationType("KTP", "-");
    public static IdentificationType Visa => new IdentificationType("VISA", "-");
    public static IdentificationType Paspor => new IdentificationType("PASPOR", "-");
    public static IdentificationType Kitas => new IdentificationType("KITAS", "-");
    public static IdentificationType Sim => new IdentificationType("SIM", "-");
    public static IdentificationType Kk => new IdentificationType("KK", "-");
}