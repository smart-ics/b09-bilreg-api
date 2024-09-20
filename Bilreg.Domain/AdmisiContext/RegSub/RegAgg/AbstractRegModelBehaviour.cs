using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public abstract partial class AbstractRegModel
{
    public void SetRegDate(DateTime dateTime) => RegDate = dateTime;

    public void SetRegOut(DateTime dateTime) => RegOutDate = dateTime;

    public void Void() => VoidDate = DateTime.Now;
    public void CancelVoid() => VoidDate = new DateTime(3000, 1, 1);

    public void SetPasien(PasienModel pasien)
    {
        PasienId = pasien.PasienId;
        NoMedRec = pasien.GetNoMedrec();
        PasienName = pasien.PasienName;
        TglLahir = pasien.TglLahir;
        Gender = pasien.Gender;
    }

    public void UsingInsurance(TipeJaminanModel tipeJaminan)
    {
        TipeJaminanId = tipeJaminan.TipeJaminanId;
        TipeJaminanName = tipeJaminan.TipeJaminanName;
        JaminanId = tipeJaminan.JaminanId;
        JaminanName = tipeJaminan.JaminanName;
    }

    public void BayarSendiri()
    {
        TipeJaminanId = "00000";
        TipeJaminanName = "UMUM [BAYAR SENDIRI]";
        
    }
    
    public void Rujukan(RujukanModel rujukan, string rujukanReffNo, DateTime rujukanDate)
    {
        CaraMasukDkId = rujukan.CaraMasukDkId;
        CaraMasukDkName = rujukan.CaraMasukDkName;
        RujukanId = rujukan.RujukanId;
        RujukanName = rujukan.RujukanName;
        RujukanReffNo = rujukanReffNo;
        RujukanDate = rujukanDate;
    }

    public void DatangSendiri()
    {
        CaraMasukDkId = "8";
        CaraMasukDkName = "DATANG SENDIRI";
        RujukanId = string.Empty;
        RujukanName = string.Empty;
        RujukanReffNo = string.Empty;
        RujukanDate = new DateTime(3000,1,1);
    }
}