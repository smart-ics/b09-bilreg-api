using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public partial class PolisModel
{
    public void WithTipeJaminan(TipeJaminanModel tipeJaminan)
    {
        Guard.IsNotNull(tipeJaminan);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanId);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanName);
        
        TipeJaminanId = tipeJaminan.TipeJaminanId;
        TipeJaminanName = tipeJaminan.TipeJaminanName;
    }

    public void WithKelas(KelasModel kelas)
    {
        Guard.IsNotNull(kelas);
        Guard.IsNotEmpty(kelas.KelasId);
        Guard.IsNotEmpty(kelas.KelasName);
        
        KelasId = kelas.KelasId;
        KelasName = kelas.KelasName;
    }

    public void CoverRawatJalan(bool value) => IsCoverRajal = value;
}