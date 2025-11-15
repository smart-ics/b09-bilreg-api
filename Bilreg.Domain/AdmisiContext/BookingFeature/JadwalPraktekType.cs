using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record JadwalPraktekType : IJadwalPraktekKey
{
    public JadwalPraktekType(string jadwalPraktekId, 
        PetugasMedisReff dokter, LayananReff layanan, LayananDkReff layanandk, GroupSpesialisType groupSpesiallis,
        DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai, int maxPasien)
    {
        JadwalPraktekId = jadwalPraktekId;
        Dokter = dokter;
        Layanan = layanan;
        LayananDk = layanandk;
        GroupSpesialis = groupSpesiallis;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
        MaxPasien = maxPasien;
    }
    public static JadwalPraktekType Default 
        => new("-", PetugasMedisType.Default.ToReff(), LayananType.Default.ToReff(), 
            LayananDkType.Default.ToReff(), GroupSpesialisType.Default,
            DayOfWeek.Monday, TimeOnly.MinValue, TimeOnly.MinValue, 0);
    public static IJadwalPraktekKey Key(string id) => Default with { JadwalPraktekId = id };
    
    public string JadwalPraktekId { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public GroupSpesialisType GroupSpesialis { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
    public int MaxPasien {  get; init; }
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}