using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapModel(
    DateOnly TglPraktek,
    string DokterId,
    string LayananId,
    TimeOnly JamJadwal,
    int NoUrut,
    PasienReff Pasien,
    RegReff Reg,
    string ReffId,
    string Flag
)
{
    #region CREATION
    public static AntrianMapModel Create(
        DateOnly tglPraktek,
        string dokterId,
        string layananId,
        TimeOnly jamPraktek,
        int noUrut,
        PasienReff pasien,
        RegReff reg,
        string reffId,
        string flag)
    {
        Guard.Against.NullOrWhiteSpace(dokterId, nameof(dokterId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));

        return new AntrianMapModel(tglPraktek, dokterId, layananId, jamPraktek, noUrut, pasien, reg, reffId, flag);
    }

    public static AntrianMapModel Default => new(
        TglPraktek: DateOnly.MinValue,
        DokterId: "-",
        LayananId: "-",
        JamJadwal: TimeOnly.MinValue,
        NoUrut: 0,
        Pasien: new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        Reg: new RegReff("-", "-", "_"),
        ReffId: "",
        Flag: ""
    );

    public static AntrianMapModel AutoSlot(DateOnly tglPraktek, string dokterId, string layananId, TimeOnly jamPraktek, int noUrut) =>
        new(
            TglPraktek: tglPraktek,
            DokterId: dokterId,
            LayananId: layananId,
            JamJadwal: jamPraktek,
            NoUrut: noUrut,
            Pasien: new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
            Reg: new RegReff("-", "-", "_"),
            ReffId: "",
            Flag: "AUTO"
        );
    #endregion

    #region BEHAVIOR
    public AntrianMapModel SetPasien(PasienReff pasien, RegReff reg, string reffId, string flag) =>
        this with { Pasien = pasien, Reg = reg, ReffId = reffId, Flag = flag };

    public AntrianMapModel Void() => this with
    {
        ReffId = "-",
        Flag = "AUTO"
    };
    #endregion
}
