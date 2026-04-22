using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

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
        string pattern,
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
        Pattern = pattern;
        MaxPasien = maxPasien;
        
        _listMap = listMap?.ToList() ?? [];
    }

    public static AntrianMapModel Default => new(
        antrianMapId: "-",
        jadwalId: "-",
        dokter: new PpaReff("-", "-"),
        layanan: new LayananReff("-", "-"),
        tglJadwal: DateOnly.MinValue,
        jamJadwal: TimeOnly.MinValue,
        jamPraktek: TimeOnly.MinValue,
        pattern: "",
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
    public string Pattern { get; init; }
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
}


public interface IAntrianMapKey
{
    string AntrianMapId { get; }
}