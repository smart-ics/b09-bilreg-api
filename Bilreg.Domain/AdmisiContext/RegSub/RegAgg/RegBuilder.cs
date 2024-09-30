using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegBuilder : AbstractRegModel
{
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
        if (tipeJaminan.TipeJaminanId == TIPEJAMINAN_UMUM_ID)
        {
            TipeJaminanName = TIPEJAMINAN_UMUM_NAME;
            JaminanId = JAMINAN_UMUM_ID;
            JaminanName = JAMINAN_UMUM_NAME;
            PolisId = string.Empty;
            KelasJaminanId = string.Empty;
            KelasJaminanName = string.Empty;
        }
        else
        {
            TipeJaminanId = polis.TipeJaminanId;
            TipeJaminanName = polis.TipeJaminanName;
            JaminanId = polis.JaminanId;
            JaminanName = polis.JaminanName;
            PolisId = polis.PolisId;
            KelasJaminanId = polis.KelasId;
            KelasJaminanName = polis.KelasName;
        }
        return this;
    }

    public RegBuilder WithCaraMasuk(CaraMasukDkModel caraMasukDk, RujukanModel rujukan)
    {
        if (caraMasukDk.CaraMasukDkId == "8")
        {
            CaraMasukDkId = CARAMASUK_DATANGSENDIRI_ID;
            CaraMasukDkName = CARAMASUK_DATANGSENDIRI_NAME;
            RujukanId = string.Empty;
            RujukanName = string.Empty;
            RujukanReffNo = string.Empty;
            RujukanDate = new DateTime(3000,1,1);
        }
        else
        {
            CaraMasukDkId = rujukan.CaraMasukDkId;
            CaraMasukDkName = rujukan.CaraMasukDkName;
            RujukanId = rujukan.RujukanId;
            RujukanName = rujukan.RujukanName;
            RujukanReffNo = rujukan.rujukanReffNo;
            RujukanDate = rujukanDate;
        }
        return this;

    }
}