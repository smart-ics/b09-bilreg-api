using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapModel : IAntrianMapKey
{
    private readonly List<AntrianMapDetilModel> _listMap;

    #region CREATION
    public AntrianMapModel(string antrianMapId, string jadwalId,
        PpaReff dokter, LayananReff layanan, 
        DateOnly tglJadwal, TimeOnly jamJadwal, TimeOnly jamPraktek,
        AntrianPatternType pattern, int maxPasien,
        IEnumerable<AntrianMapDetilModel> listMap)
    {
        AntrianMapId = antrianMapId;
        JadwalId = jadwalId;
        Dokter = dokter;
        Layanan = layanan;
        TglJadwal = tglJadwal;
        JamJadwal = jamJadwal;
        JamPraktek = jamPraktek;
        AntrianPattern = pattern;
        MaxPasien = maxPasien;
        
        _listMap = listMap?.ToList() ?? [];
    }

    public static AntrianMapModel CreateFromJadwal(JadwalPraktekType jadwal,
        DateOnly tgl)
    {
        var newKey = NunaId.New("ANM");
        var result = new AntrianMapModel(
            antrianMapId: newKey,
            jadwalId    : jadwal.JadwalPraktekId,
            dokter      : jadwal.Dokter,
            layanan     : jadwal.Layanan,
            tglJadwal   : tgl,
            jamJadwal   : jadwal.JamMulai,
            jamPraktek  : jadwal.JamMulai,
            pattern     : jadwal.AntrianPattern,
            maxPasien   : jadwal.MaxPasien,
            listMap: []
        );
        return result;
    }
    
    public static AntrianMapModel Default => new(
        antrianMapId: "-",
        jadwalId: "-",
        dokter: new PpaReff("-", "-"),
        layanan: new LayananReff("-", "-"),
        tglJadwal: DateOnly.MinValue,
        jamJadwal: TimeOnly.MinValue,
        jamPraktek: TimeOnly.MinValue,
        pattern: AntrianPatternType.Default,
        maxPasien: 0,
        listMap: []
    );
    
    public static IAntrianMapKey Key(string id)
    {
        var result = Default with {AntrianMapId = id};
        return result;
    }

    #endregion

    #region PROPERTIES
    public string AntrianMapId { get; init; }
    public string JadwalId { get; init; }
    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public DateOnly TglJadwal { get; init; }
    public TimeOnly JamJadwal { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public AntrianPatternType AntrianPattern { get; init; }
    public int MaxPasien { get; private set; }
    public int LastNoUrut => _listMap.Max(x => x.NoUrut);
    public IEnumerable<AntrianMapDetilModel> ListMap => _listMap;

    public string DokterId => Dokter.PpaId;
    public string LayananId => Layanan.LayananId;
    #endregion

    #region BEHAVIOR

    public void VoidSlot(int noUrut)
    {
        var detil = _listMap.FirstOrDefault(x => x.NoUrut == noUrut);
        detil?.Void();
    }
    public void SetDataPasien(int noUrut, RegModel reg)
    {
        var detil = _listMap.FirstOrDefault(x => x.NoUrut == noUrut);
        if (detil is null)
            return;
        
        detil.SetPasien(reg.Pasien.PasienName,
            reg.Pasien.PasienId, reg.RegId);
    }
    public void SetDataPasien(int noUrut, BookingModel booking)
    {
        var detil = _listMap.FirstOrDefault(x => x.NoUrut == noUrut);
        if (detil is null)
            return;

        detil.SetPasien(booking.Person.PersonName, 
            booking.PasienId,
            booking.BookingId);
    }
    
    public AntrianMapDetilModel GetNextAntrian(string flag)
    {
        var detil = _listMap
            .Where(x => !x.IsTerpakai)
            .Where(x => x.Flag == flag)
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();

        var result = detil ?? AntrianMapDetilModel.AutoSlot(LastNoUrut + 1);
        return result;            
    }

    public int TotalSlotCount => _listMap.Count;

    public void SeedingMap()
    {
        if (AntrianPattern.Tipe == "FLAG")
            SeedingMapFlag();
        else
            SeedingMapAuto(); 
    }
    
    public void AttachDetil(IEnumerable<AntrianMapDetilModel> listDetil)
    {
        _listMap.Clear();
        _listMap.AddRange(listDetil);
    }

    public AntrianMapDetilModel AddAuto(RegModel reg)
    {
        var newDetil = AntrianMapDetilModel.AutoSlot(LastNoUrut + 1)
            .SetPasien(reg.Pasien.PasienName, reg.Pasien.PasienId, reg.RegId);
        return newDetil;
    }
    public AntrianMapDetilModel AddAuto(BookingModel booking)
    {
        var newDetil = AntrianMapDetilModel.AutoSlot(LastNoUrut + 1)
            .SetPasien(booking.Person.PersonName, booking.PasienId, booking.BookingId);
        return newDetil;
    }
    

    #endregion
    private void SeedingMapAuto()
    {
        _listMap.Clear();
        var noUrut = 1;
        while (noUrut <= MaxPasien)
        {
            var newItem = new AntrianMapDetilModel(noUrut, string.Empty, string.Empty, string.Empty, "AUTO", false);
            _listMap.Add(newItem );
            noUrut ++;
        }
    }

    private void SeedingMapFlag()
    {
        _listMap.Clear();
        var noUrut = 1;
        while (noUrut <= MaxPasien)
        {
            var listFullCycle = FullCyclePatternSeed(AntrianPattern, ref noUrut);
            if (listFullCycle.Count == 0)
                break;
            _listMap.AddRange(listFullCycle);
        }
        _listMap.RemoveAll(x => x.NoUrut > MaxPasien);
    }

    private static List<AntrianMapDetilModel> FullCyclePatternSeed(AntrianPatternType pattern, ref int startNumber)
    {
        var result = new List<AntrianMapDetilModel>();
        foreach(var item in pattern.Pttrn)
            for (var i = 0; i < item.Qty; i++)
            {
                result.Add(new AntrianMapDetilModel(startNumber, string.Empty, string.Empty, string.Empty, item.Desc, false));
                startNumber++;                
            }
        return result;
    }
}


public interface IAntrianMapKey
{
    string AntrianMapId { get; }
}