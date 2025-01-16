using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public partial class RegModel : IRegKey
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";
    
    public string RegId { get; private set; }
    public JenisRegEnum JenisReg { get; private set; }
    public TglJamTrsVo TglJamTrs { get; private set; }
    public ActivityFlagVo VoidFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");

    public RegPasienVo Pasien { get; private set; }
    public RegTipeJaminanVo TipeJaminan { get; private set; }
    public RegCaraMasukVo CaraMasuk { get; private set; }
    public KarcisTarifVo KarcisTarif { get; private set; }
    public RegKelasVo Kelas { get; private set; }
    
    #region METHOD
    public RegModel(string regId)
    {
        RegId = regId;
    }

    public RegModel(string regId, TglJamTrsVo tglJamTrs,
        ActivityFlagVo voidFlag, RegPasienVo pasien, 
        RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk)
    {
        RegId = regId;
        TglJamTrs = tglJamTrs;
        VoidFlag = voidFlag;

        Pasien = pasien;
        TipeJaminan = tipeJaminan;
        CaraMasuk = caraMasuk;
    }

    public void SetTglJamTrs(TglJamTrsVo tglJamTrs)
    {
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        TglJamTrs = tglJamTrs;
    }


    public void SetVoidFlag(ActivityFlagVo voidFlag)
    {
        if (voidFlag.ActivityDate == DefaultDate)
            throw new ArgumentException("VoidDate invalid");
        VoidFlag = voidFlag;
    }
    
    public void SetPasien(RegPasienVo pasien)
    {
        Guard.IsNotNull(pasien);
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        Pasien = pasien;
    }
    
    public void SetJaminan(RegTipeJaminanVo tipeJaminan)
    {
        Guard.IsNotNull(tipeJaminan);
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        TipeJaminan = tipeJaminan;
    }

    public void SetCaraMasuk(RegCaraMasukVo caraMasuk)
    {
        Guard.IsNotNull(caraMasuk);
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        CaraMasuk = caraMasuk;
    }
    
    public void SetKarcisTarif(KarcisTarifVo karcisTarif)
    {
        Guard.IsNotNull(karcisTarif);
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        KarcisTarif = karcisTarif;
    }

    public void SetKelas(RegKelasVo regKelas)
    {
        Guard.IsNotNull(regKelas);
        if (VoidFlag.IsFlagged)
            throw new ArgumentException("Register sudah void");

        Kelas = regKelas;            
    }
    #endregion    
}