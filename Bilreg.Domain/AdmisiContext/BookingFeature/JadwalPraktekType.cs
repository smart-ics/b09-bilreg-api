using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record JadwalPraktekType : IJadwalPraktekKey
{
    public JadwalPraktekType(string jadwalPraktekId, 
        PetugasMedisReff dokter, LayananReff layanan, 
        DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai)
    {
        JadwalPraktekId = jadwalPraktekId;
        Dokter = dokter;
        Layanan = layanan;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
    }
    public string JadwalPraktekId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
    
    public static JadwalPraktekType Create(PetugasMedisType dokter,
        LayananReff layanan, DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai)
    {
        var newId = Ulid.NewUlid().ToString();
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(dokter.Smf, nameof(dokter.Smf));
        
        return new JadwalPraktekType(newId, dokter.ToReff(), layanan, hari, jamMulai, jamSelesai);
    }
    
    public static JadwalPraktekType Default =>
        new JadwalPraktekType("", PetugasMedisType.Default.ToReff(), LayananType.Default.ToReff(), DayOfWeek.Monday, 
            new TimeOnly(0, 0), new TimeOnly(0, 0));
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}