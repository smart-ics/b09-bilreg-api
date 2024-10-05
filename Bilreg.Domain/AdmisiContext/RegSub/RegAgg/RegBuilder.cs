﻿using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegBuilder : AbstractRegModel
{
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";

    public RegBuilder SetRegId(string regId)
    {
        RegId = regId;
        return this;
    }
    
    public RegBuilder SetUser(string userId)
    {
        UserId = userId;
        return this;
    }
    
    public RegBuilder SetRegDate(DateTime dateTime)
    {
        RegDate = dateTime;
        return this;
    }

    public RegBuilder Discharge(DateTime regOutDate, string userId)
    {
        RegOutDate = regOutDate;
        UserIdOut = userId;
        return this;
    }

    public RegBuilder Void(string userId)
    {
        VoidDate = DateTime.Now;
        UserIdVoid = userId;
        return this;
    }

    public RegBuilder CancelVoid()
    {
        VoidDate = new DateTime(3000, 1, 1);
        UserIdVoid = string.Empty;
        return this;
    }

    public RegBuilder WithPasien(PasienModel pasien)
    {
        Guard.IsNotNull(pasien);
        
        PasienId = pasien.PasienId;
        NoMedRec = pasien.GetNoMedrec();
        PasienName = pasien.PasienName;
        TglLahir = pasien.TglLahir;
        Gender = pasien.Gender;
        return this;
    }

    public RegBuilder WithJaminan(TipeJaminanModel tipeJaminan, PolisModel polis)
    {
        TipeJaminan = new RegTipeJaminanVo(tipeJaminan, polis);
        return this;
    }

    public RegBuilder WithCaraMasuk(CaraMasukDkModel caraMasukDk, RujukanModel rujukan)
    {
        CaraMasuk = new RegCaraMasukVo(caraMasukDk, rujukan);
        return this;
    }

    public RegBuilder WithLampiran(LampiranRujukanVo lampiran)
    {
        if (CaraMasuk.CaraMasukDkId == CARAMASUK_DATANGSENDIRI_ID)
            throw new ArgumentException($"Cara Masuk Datang Sendiri tidak perlu Lampuran Rujukan");
        LampiranRujukan = lampiran;
        return this;
    }
}