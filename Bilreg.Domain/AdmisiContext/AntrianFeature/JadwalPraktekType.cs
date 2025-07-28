using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record JadwalPraktekType : IJadwalPraktekKey
{
    public JadwalPraktekType(string jadwalPraktekId, 
        PetugasMedisReff dokter, SmfType smf, DayOfWeek hari, 
        TimeSpan jamMulai, TimeSpan jamSelesai)
    {
        JadwalPraktekId = jadwalPraktekId;
        Dokter = dokter;
        Smf = smf;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
    }
    public string JadwalPraktekId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public SmfType Smf { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeSpan JamMulai { get; init; }
    public TimeSpan JamSelesai { get; init; }

    public static JadwalPraktekType Create(PetugasMedisType dokter,
        DayOfWeek hari, TimeSpan jamMulai, TimeSpan jamSelesai)
    {
        var newId = Ulid.NewUlid().ToString();
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(dokter.Smf, nameof(dokter.Smf));
        
        return new JadwalPraktekType(newId, dokter.ToReff(), dokter.Smf, hari, jamMulai, jamSelesai);
    }
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}