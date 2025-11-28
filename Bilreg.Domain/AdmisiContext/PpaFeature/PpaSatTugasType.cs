using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public class PpaSatTugasType
{
    public PpaSatTugasType(SatTugasType satTugas, bool isUtama)
    {
        Guard.Against.Null(satTugas);
        SatTugas = satTugas;
        IsUtama = isUtama;
    }
    public SatTugasType SatTugas { get; init; }
    public bool IsUtama { get; init; }
}