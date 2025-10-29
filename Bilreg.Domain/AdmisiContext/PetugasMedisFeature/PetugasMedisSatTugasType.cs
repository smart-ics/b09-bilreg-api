using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

public class PetugasMedisSatTugasType
{
    public PetugasMedisSatTugasType(SatTugasType satTugas, bool isUtama)
    {
        Guard.Against.Null(satTugas);
        SatTugas = satTugas;
        IsUtama = isUtama;
    }
    public SatTugasType SatTugas { get; init; }
    public bool IsUtama { get; protected set; }
}