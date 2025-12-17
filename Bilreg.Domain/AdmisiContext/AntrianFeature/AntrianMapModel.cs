using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapModel : IAntrianMapKey
{
    private readonly List<AntrianMapSlotModel> _listSlot;

    #region CREATION
    public AntrianMapModel(PpaReff dokter, LayananReff layanan, 
        DateOnly tglPraktek, TimeOnly jamPraktek, IEnumerable<AntrianMapSlotModel> listSlot)
    {
        Dokter = dokter;
        Layanan = layanan;
        TglPraktek = tglPraktek;
        JamPraktek = jamPraktek;
        _listSlot = listSlot?.ToList() ?? [];
    }

    public static AntrianMapModel Create(PpaReff dokter, LayananReff layanan, 
        DateOnly tglPraktek, TimeOnly jamPraktek, IEnumerable<AntrianMapSlotModel> listSlot)
    {
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(layanan, nameof(layanan));

        return new AntrianMapModel(dokter, layanan, tglPraktek, jamPraktek, listSlot);
    }

    public static AntrianMapModel Default => new(
        new PpaReff("-", "-"),
        new LayananReff("-", "-"),
        DateOnly.MinValue,
        TimeOnly.MinValue,
        []
    );

    public static IAntrianMapKey Key(string ppaId, string layananId, DateOnly tglPraktek, TimeOnly jamPraktek) =>
        Default with { Dokter = new PpaReff(ppaId, "-"), 
            Layanan = new LayananReff(layananId, "-"), 
            TglPraktek = tglPraktek, 
            JamPraktek = jamPraktek };
    #endregion

    #region PROPERTIES
    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public DateOnly TglPraktek { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public IEnumerable<AntrianMapSlotModel> ListSlot => _listSlot;

    // Implementasi IAntrianMapKey
    public string PpaId => Dokter.PpaId;
    public string LayananId => Layanan.LayananId;
    #endregion

    #region BEHAVIOUR
    
    public void GenerateSlot(int jumlah)
    {
        Guard.Against.NegativeOrZero(jumlah);

        var slots =  Enumerable.Range(1, jumlah)
            .Select(i => new AntrianMapSlotModel(
                NoUrut: i,
                Pasien: new PasienReff("-", "-", new DateOnly(3000,1,1), "-"),
                Reg: new RegReff("-", "-", "_"),
                ReffId: "-",
                Flag: "-"
            ));
        _listSlot.AddRange(slots);
    }
    #endregion
}

public record AntrianMapSlotModel(int NoUrut, PasienReff Pasien, RegReff Reg, string ReffId, string Flag);

public interface IAntrianMapKey
{
    string PpaId { get; }
    string LayananId { get; }
    DateOnly TglPraktek { get; }
    TimeOnly JamPraktek { get; }
}