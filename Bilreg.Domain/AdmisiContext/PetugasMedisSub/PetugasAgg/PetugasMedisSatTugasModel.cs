using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisSatTugasModel
{
    public PetugasMedisSatTugasModel(SatTugasType satTugas, bool isUtama)
    {
        Guard.Against.Null(satTugas);
        SatTugas = satTugas;
        IsUtama = isUtama;
    }
    public SatTugasType SatTugas { get; init; }
    public bool IsUtama { get; protected set; }
}