using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record JadwalPraktekType : IJadwalPraktekKey
{
    public JadwalPraktekType(string jadwalPraktekId, 
        PetugasMedisReff dokter, LayananReff layanan, LayananDkReff layanandk,
        DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai)
    {
        JadwalPraktekId = jadwalPraktekId;
        Dokter = dokter;
        Layanan = layanan;
        LayananDk = layanandk;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
    }
    public static JadwalPraktekType Default 
        => new("-", PetugasMedisType.Default.ToReff(), LayananType.Default.ToReff(), 
            LayananDkType.Default.ToReff(),
            DayOfWeek.Monday, TimeOnly.MinValue, TimeOnly.MinValue);
    public static IJadwalPraktekKey Key(string id) => Default with { JadwalPraktekId = id };
    
    public string JadwalPraktekId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}