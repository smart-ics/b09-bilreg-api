using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record JadwalPraktekType : IJadwalPraktekKey
{
    public JadwalPraktekType(string jadwalPraktekId, 
        PpaReff dokter, LayananReff layanan, LayananDkReff layanandk, 
        GroupSpesialisType groupSpesiallis, RuangType ruang,
        DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai, int maxPasien,
        AntrianPatternType antrianPattern)
    {
        JadwalPraktekId = jadwalPraktekId;
        Dokter = dokter;
        Layanan = layanan;
        LayananDk = layanandk;
        GroupSpesialis = groupSpesiallis;
        Ruang = ruang;
        Hari = hari;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
        MaxPasien = maxPasien;
        AntrianPattern = antrianPattern;
    }
    public static JadwalPraktekType Default 
        => new("-", PpaType.Default.ToReff(), LayananType.Default.ToReff(), 
            LayananDkType.Default.ToReff(), GroupSpesialisType.Default, RuangType.Default,
            DayOfWeek.Monday, TimeOnly.MinValue, TimeOnly.MinValue, 0, AntrianPatternType.Default);
    public static IJadwalPraktekKey Key(string id) => Default with { JadwalPraktekId = id };
    
    public string JadwalPraktekId { get; init; }
    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public GroupSpesialisType GroupSpesialis { get; init; }
    public RuangType Ruang { get; init; }
    public DayOfWeek Hari { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
    public AntrianPatternType AntrianPattern { get; init; }
    public int MaxPasien {  get; init; }
}

public interface IJadwalPraktekKey
{
    string JadwalPraktekId { get; }
}

