using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public class PpaLayananType
{
    public PpaLayananType(LayananReff layanan, bool isUtama)
    {
        Guard.Against.Null(layanan);
        
        Layanan = layanan;
        IsUtama = isUtama;
    }

    public LayananReff Layanan { get; init; }
    public bool IsUtama { get; init; }
}

public record PpaLayananView(
    string PpaId,
    string PpaName,
    LayananReff Layanan,
    GroupSpesialisType GroupSpesialis);