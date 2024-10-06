using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegJalanBuilder
{
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";
    private RegJalanModel _agg;

    public RegJalanBuilder Create(string regId)
    {
        _agg = new RegJalanModel(regId);
        return this;
    }
    
    public RegJalanBuilder TglJamTrs(DateTime tglJam, string userId)
    {
        var tglJamTrs = new TglJamTrsVo(tglJam, userId);
        _agg.SetTglJamTrs(tglJamTrs);
        return this;
    }
    
    public RegJalanBuilder RegOut(DateTime regOutDate, string userId)
    {
        var regOutFlag = new RegOutFlagVo(regOutDate, userId);
        _agg.SetRegOutFlag(regOutFlag);
        return this;
    }

    public RegJalanBuilder CancelOut()
    {
        var regOUtFlag = new RegOutFlagVo(new DateTime(3000, 1, 1), string.Empty);
        _agg.SetRegOutFlag(regOUtFlag);
        return this;
    }

    public RegJalanBuilder Void(DateTime tglJam,string userId)
    {
        var voidFlag = new VoidFlagVo(tglJam, userId);
        _agg.SetVoidFlag(voidFlag);
        return this;
    }

    public RegJalanBuilder CancelVoid()
    {
        var voidFlag = new VoidFlagVo(new DateTime(3000, 1, 1), string.Empty);
        _agg.SetVoidFlag(voidFlag);
        return this;
    }

    public RegJalanBuilder SetPasien(PasienModel pasien)
    {
        Guard.IsNotNull(pasien);
        var px = new RegPasienVo(pasien);
        _agg.SetPasien(px);
        return this;
    }

    public RegJalanBuilder SetJaminan(TipeJaminanModel tipeJaminan, PolisModel polis)
    {
        var tipeJmn = new RegTipeJaminanVo(tipeJaminan, polis);
        _agg.SetJaminan(tipeJmn);
        return this;
    }

    public RegJalanBuilder WithCaraMasuk(CaraMasukDkModel caraMasukDk, RujukanModel rujukan)
    {
        var caraMasuk = new RegCaraMasukVo(caraMasukDk, rujukan);
        _agg.SetCaraMasuk(caraMasuk);
        return this;
    }

    public RegJalanBuilder WithLampiran(LampiranRujukanVo lampiran)
    {
        _agg.SetLampiranRujukan(lampiran);
        return this;
    }
}