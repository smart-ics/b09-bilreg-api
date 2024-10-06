using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public abstract class AbstractRegModel : IRegKey
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";
    
    public string RegId { get; private set; }
    public JenisRegEnum JenisReg { get; private set; }
    public TglJamTrsVo TglJamTrs { get; private set; }
    public VoidFlagVo VoidFlag { get; private set; }
    public RegOutFlagVo RegOutFlag { get; private set; }

    public RegPasienVo Pasien { get; private set; }
    public RegTipeJaminanVo TipeJaminan { get; private set; }
    public RegCaraMasukVo CaraMasuk { get; private set; }
    public LampiranRujukanVo LampiranRujukan { get; private set; }
    
    
    protected AbstractRegModel(string regId)
    {
        RegId = regId;
    }

    protected AbstractRegModel(string regId, TglJamTrsVo tglJamTrs,
        VoidFlagVo voidStamp, RegOutFlagVo regOut, RegPasienVo pasien, 
        RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk,
        LampiranRujukanVo lampiranRujukan)
    {
        RegId = regId;
        TglJamTrs = tglJamTrs;
        VoidFlag = voidStamp;
        RegOutFlag = regOut;

        Pasien = pasien;
        TipeJaminan = tipeJaminan;
        CaraMasuk = caraMasuk;
        LampiranRujukan = lampiranRujukan;
    }

    public void SetTglJamTrs(TglJamTrsVo tglJamTrs)
    {
        if (VoidFlag.IsVoid)
            throw new ArgumentException("Register sudah void");

        TglJamTrs = tglJamTrs;
    }


    public void SetRegOutFlag(RegOutFlagVo regOutFlag)
    {
        if (regOutFlag.RegOutDate < TglJamTrs.TglJam)
            throw new ArgumentException("RegOutDate invalid, harus setelah tanggal masuk");

        if (VoidFlag.IsVoid)
            throw new ArgumentException("Register sudah void");
        
        RegOutFlag = regOutFlag;
    }

    public void SetVoidFlag(VoidFlagVo voidFlag)
    {
        if (voidFlag.VoidDate == DefaultDate)
            throw new ArgumentException("VoidDate invalid");
        VoidFlag = voidFlag;
    }
    
    public void SetPasien(RegPasienVo pasien)
    {
        Guard.IsNotNull(pasien);
        Pasien = pasien;
    }
    public void SetJaminan(RegTipeJaminanVo tipeJaminan)
    {
        Guard.IsNotNull(tipeJaminan);
        TipeJaminan = tipeJaminan;
    }

    public void SetCaraMasuk(RegCaraMasukVo caraMasuk)
    {
        Guard.IsNotNull(caraMasuk);
        CaraMasuk = caraMasuk;
    }
    
    public void SetLampiranRujukan(LampiranRujukanVo lampiranRujukan)
    {
        Guard.IsNotNull(lampiranRujukan);
        if (CaraMasuk.CaraMasukDkId == CARAMASUK_DATANGSENDIRI_ID)
            throw new ArgumentException($"Cara Masuk Datang Sendiri tidak perlu Lampuran Rujukan");
        
        LampiranRujukan = lampiranRujukan;
    }

}

public interface IRegKey
{
    string RegId { get; }
}