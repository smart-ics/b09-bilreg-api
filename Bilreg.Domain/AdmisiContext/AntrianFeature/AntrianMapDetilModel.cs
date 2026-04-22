using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapDetilModel
{
    #region CREATION

    public AntrianMapDetilModel(int noUrut, PasienReff pasien, 
        RegReff reg, string reffId, string flag, bool isTerpakai)
    {
        NoUrut = noUrut;
        Pasien = pasien;
        Reg = reg;
        ReffId = reffId;
        Flag = flag;
        IsTerpakai = isTerpakai;
    }

    public static AntrianMapDetilModel Default => new(
        0, 
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        new RegReff("-", "-", "_"),
        "", "", false
    );

    public static AntrianMapDetilModel AutoSlot(int noUrut)
    {
        var result = new AntrianMapDetilModel(
            noUrut, 
            new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
            new RegReff("-", "-", "_"),
            "-", "AUTO", false
        );
        return result;
    }
    #endregion

    #region PROPERTY
    public int NoUrut { get ;init; }
    public PasienReff Pasien { get ; private set; }
    public RegReff Reg { get ;private set; }
    public string ReffId { get; private set; }
    public string Flag { get; private set; }
    public bool IsTerpakai { get; private set; }
    #endregion
    
    #region BEHAVIOR
    public AntrianMapDetilModel SetPasien(PasienReff pasien, RegReff reg, string reffId, string flag)
    {
        Pasien = pasien;
        Reg = reg; 
        ReffId = reffId;  
        Flag = flag; 
        IsTerpakai = true;
        return this;
    }

    internal void Void()
    {
        Pasien = PasienModel.Default.ToReff();
        Reg = RegModel.Default.ToReff();
        ReffId = "-";
        Flag = "AUTO";
    }
    #endregion
}