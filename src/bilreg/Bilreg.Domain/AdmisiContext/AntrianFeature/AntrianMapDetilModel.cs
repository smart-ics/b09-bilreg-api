using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianMapDetilModel
{
    #region CREATION

    public AntrianMapDetilModel(int noUrut, 
        string pasienName, string pasienId, string reffId, 
        string flag, bool isTerpakai)
    {
        NoUrut = noUrut;
        PasienName = pasienName;
        PasienId = pasienId;
        ReffId = reffId;
        Flag = flag;
        IsTerpakai = isTerpakai;
    }

    public static AntrianMapDetilModel Default => new(
        0, string.Empty, string.Empty, 
        "", "", false
    );

    internal static AntrianMapDetilModel AutoSlot(int noUrut)
    {
        var result = new AntrianMapDetilModel(
            noUrut, string.Empty, string.Empty, 
            "-", "AUTO", false
        );
        return result;
    }
    #endregion

    #region PROPERTY
    public int NoUrut { get ;init; }
    public string PasienId { get; private set; }
    public string PasienName { get; private set; }
    public string ReffId { get; private set; }
    public string Flag { get; private set; }
    public bool IsTerpakai { get; private set; }
    #endregion
    
    #region BEHAVIOR
    public AntrianMapDetilModel SetPasien(
        string pasienName, string pasienId, string reffId)
    {
        PasienName = pasienName;
        PasienId = pasienId;
        ReffId = reffId;  
        IsTerpakai = true;
        return this;
    }

    public bool IsFreeSlot() => !IsTerpakai && ReffId.Trim() == "";

    internal void Void()
    {
        PasienId = "-";
        PasienName = "-";
        ReffId = "";
        Flag = "AUTO";
        IsTerpakai = false;
    }
    #endregion
}