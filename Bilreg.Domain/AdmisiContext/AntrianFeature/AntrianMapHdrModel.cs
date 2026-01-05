using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapHdrModel : IAntrianMapHdrKey
{
    private readonly List<AntrianMapModel> _listMap;

    #region CREATION
    private AntrianMapHdrModel(
        string jadwalId,
        PpaReff dokter,
        LayananReff layanan,
        DateOnly tglJadwal,
        TimeOnly jamJadwal,
        TimeOnly jamPraktek,
        IEnumerable<AntrianMapModel> listMap)
    {
        JadwalId = jadwalId;
        Dokter = dokter;
        Layanan = layanan;
        TglJadwal = tglJadwal;
        JamJadwal = jamJadwal;
        JamPraktek = jamPraktek;
        _listMap = listMap?.ToList() ?? [];
    }

    public static AntrianMapHdrModel Create(
        string jadwalId,
        PpaReff dokter,
        LayananReff layanan,
        DateOnly tglJadwal,
        TimeOnly jamJadwal,
        TimeOnly jamPraktek,
        IEnumerable<AntrianMapModel> listMap)
    {
        Guard.Against.NullOrWhiteSpace(jadwalId, nameof(jadwalId));
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(layanan, nameof(layanan));

        return new AntrianMapHdrModel(jadwalId, dokter, layanan, tglJadwal, jamJadwal, jamPraktek, listMap);
    }

    public static AntrianMapHdrModel Default => new(
        jadwalId: "-",
        dokter: new PpaReff("-", "-"),
        layanan: new LayananReff("-", "-"),
        tglJadwal: DateOnly.MinValue,
        jamJadwal: TimeOnly.MinValue,
        jamPraktek: TimeOnly.MinValue,
        listMap: []
    );

    public static IAntrianMapHdrKey Key(string jadwalId, DateOnly tglJadwal,
        string dokterId, string layananId, TimeOnly jamJadwal) =>
        new AntrianMapHdrModel(jadwalId, new PpaReff(dokterId, "-"),
            new LayananReff(layananId, "-"), tglJadwal, jamJadwal,
            TimeOnly.MinValue, []);

    #endregion

    #region PROPERTIES
    public string JadwalId { get; init; }
    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public DateOnly TglJadwal { get; init; }
    public TimeOnly JamJadwal { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public IEnumerable<AntrianMapModel> ListMap => _listMap;

    public string DokterId => Dokter.PpaId;
    public string LayananId => Layanan.LayananId;
    #endregion

    #region BEHAVIOR
    public void GenerateSlot(int jumlah)
    {
        Guard.Against.NegativeOrZero(jumlah, nameof(jumlah));

        if (_listMap.Any())
            throw new InvalidOperationException("Slot sudah pernah digenerate");

        var slots = Enumerable.Range(1, jumlah)
            .Select(i => AntrianMapModel.AutoSlot(TglJadwal, Dokter.PpaId, Layanan.LayananId, JamJadwal, i));

        _listMap.AddRange(slots);
    }

    public void SetDataPasien(
        int noUrut,
        PasienReff pasien,
        RegReff reg,
        string reffId,
        string flag)
    {
        var index = _listMap.FindIndex(x => x.NoUrut == noUrut);
        if (index == -1)
            throw new KeyNotFoundException($"Slot {noUrut} tidak ditemukan");

        var updatedSlot = _listMap[index].SetPasien(pasien, reg, reffId, flag);
        var newList = _listMap.ToList();
        newList[index] = updatedSlot;
        
        _listMap.Clear();
        _listMap.AddRange(newList);
    }

    public void VoidSlot(int noUrut)
    {
        var index = _listMap.FindIndex(x => x.NoUrut == noUrut);
        if (index == -1) 
            return;

        var voidedSlot = _listMap[index].Void();
        var newList = _listMap.ToList();
        newList[index] = voidedSlot;
        
        var noUrutNew = newList.Max(x => x.NoUrut) + 1;
        var newSlot = AntrianMapModel.AutoSlot(TglJadwal, Dokter.PpaId, Layanan.LayananId, 
            JamJadwal, noUrutNew);
        newList.Add(newSlot);


        _listMap.Clear();
        _listMap.AddRange(newList);
    }

    public int GetNextNoAntrian()
    {
        return _listMap
            .Where(x => string.IsNullOrEmpty(x.ReffId))
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(1)
            .Min();
    }

    public int TotalSlotCount => _listMap.Count;

    public IEnumerable<AntrianMapModel> GetAllSlots() => _listMap;

    #endregion
}


public interface IAntrianMapHdrKey
{
    string JadwalId { get; }
    DateOnly TglJadwal { get; }
    string DokterId { get; }
    string LayananId { get; } 
    TimeOnly JamJadwal { get; }
}