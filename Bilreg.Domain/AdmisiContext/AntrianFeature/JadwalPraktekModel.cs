using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

public class JadwalPraktekModel
{
    public JadwalPraktekModel(string jadwalId, 
        PetugasMedisReff dokter, DayOfWeek hari, 
        TimeSpan jamMulai, TimeSpan jamSelesai)
    {
        JadwalId = jadwalId;
        Dokter = dokter;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
    }
    public string JadwalId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeSpan JamMulai { get; init; }
    public TimeSpan JamSelesai { get; init; }

    public static JadwalPraktekModel Create(PetugasMedisReff dokter,
        DayOfWeek hari, TimeSpan jamMulai, TimeSpan jamSelesai)
    {
        var newId = Ulid.NewUlid().ToString();
        return new JadwalPraktekModel(newId, dokter, hari, jamMulai, jamSelesai);
    }
}