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

public record AntrianPatternType
{
    private readonly List<AntrianPatternItemType> _listPttrn;
    public AntrianPatternType(string tipe, int max, int rsrvd
        , IEnumerable<AntrianPatternItemType> listPattrn)
    {
        Tipe = tipe;
        Max = max;
        Rsrvd = rsrvd;
        _listPttrn = listPattrn.ToList()?.ToList() ?? [];
    }

    public static AntrianPatternType Default =>
        new AntrianPatternType(string.Empty, 0, 0, []);
    public string Tipe { get; init; } 
    public int Max { get; init; } 
    public int Rsrvd { get; init; }
    public List<AntrianPatternItemType> Pttrn => _listPttrn;
}

public record AntrianPatternItemType
{
    public AntrianPatternItemType(string desc, int qty)
    {
        Desc = desc;
        Qty = qty;
    }
    public static AntrianPatternItemType Default 
    => new AntrianPatternItemType(string.Empty, 0);
    public string Desc { get; init; } 
    public int Qty { get; init; } 
}