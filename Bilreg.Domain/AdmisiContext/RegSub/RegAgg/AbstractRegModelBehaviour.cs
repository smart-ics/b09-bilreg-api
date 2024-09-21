using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegBuilder : AbstractRegModel
{
    public RegBuilder Create(string regId, string userId)
    {
        RegId = regId;
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
        PasienId = pasien.PasienId;
        NoMedRec = pasien.GetNoMedrec();
        PasienName = pasien.PasienName;
        TglLahir = pasien.TglLahir;
        Gender = pasien.Gender;
        return this;
    }
    
    public RegBuilder UsingAsuransi(TipeJaminanModel tipeJaminan)
    {
        TipeJaminanId = tipeJaminan.TipeJaminanId;
        TipeJaminanName = tipeJaminan.TipeJaminanName;
        JaminanId = tipeJaminan.JaminanId;
        JaminanName = tipeJaminan.JaminanName;
        return this;
    }

    public RegBuilder UsingBayarSendiri()
    {
        TipeJaminanId = TIPEJAMINAN_UMUM_ID;
        TipeJaminanName = TIPEJAMINAN_UMUM_NAME;
        JaminanId = JAMINAN_UMUM_ID;
        JaminanName = JAMINAN_UMUM_NAME;
        return this;
    }

    public RegBuilder AsPasienRujukan(RujukanModel rujukan, string rujukanReffNo, DateTime rujukanDate)
    {
        CaraMasukDkId = rujukan.CaraMasukDkId;
        CaraMasukDkName = rujukan.CaraMasukDkName;
        RujukanId = rujukan.RujukanId;
        RujukanName = rujukan.RujukanName;
        RujukanReffNo = rujukanReffNo;
        RujukanDate = rujukanDate;
        return this;
    }

    public RegBuilder  AsPasienDatangSendiri()
    {
        CaraMasukDkId = CARAMASUK_DATANGSENDIRI_ID;
        CaraMasukDkName = CARAMASUK_DATANGSENDIRI_NAME;
        RujukanId = string.Empty;
        RujukanName = string.Empty;
        RujukanReffNo = string.Empty;
        RujukanDate = new DateTime(3000,1,1);
        return this;
    }
}