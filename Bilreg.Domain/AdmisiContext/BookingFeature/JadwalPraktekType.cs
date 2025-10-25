using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
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
    public static JadwalPraktekType Default 
        => new("-", PetugasMedisType.Default.ToReff(), LayananType.Default.ToReff(), 
            DayOfWeek.Monday, TimeOnly.MinValue, TimeOnly.MinValue);
    public static IJadwalPraktekKey Key(string id) => Default with { JadwalPraktekId = id };
    
    public string JadwalPraktekId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}