using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapModel : IAntrianMapKey
{
    private readonly List<AntrianMapDetilModel> _listMap;

    #region CREATION
    public AntrianMapModel(
        string antrianMapId,
        string jadwalId,
        PpaReff dokter,
        LayananReff layanan,
        DateOnly tglJadwal,
        TimeOnly jamJadwal,
        TimeOnly jamPraktek,
        AntrianPatternType pattern,
        int maxPasien,
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
        var newKey = Ulid.NewUlid().ToString();
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
        result.SeedingMap();
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

    #endregion

    private void SeedingMap()
    {
        _listMap.Clear();
        var noUrut = 1;
        foreach(var item in AntrianPattern.Pttrn)
            for (var i = 0; i < item.Qty; i++)
            {
                _listMap.Add(new AntrianMapDetilModel(noUrut, string.Empty, string.Empty, string.Empty, item.Desc, false));
                noUrut++;
                if (noUrut > MaxPasien)
                    break;
            }
        
    }
}


public interface IAntrianMapKey
{
    string AntrianMapId { get; }
}