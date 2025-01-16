using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegOutModel : RegModel
{
    public RegOutModel(string regId) : base(regId)
    {
    }

    public RegOutModel(string regId, TglJamTrsVo tglJamTrs, ActivityFlagVo voidFlag, RegPasienVo pasien, 
        RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk) : base(regId, tglJamTrs, voidFlag, pasien, tipeJaminan, caraMasuk)
    {
    }
    
    public ActivityFlagVo RegOutFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
    public ActivityFlagVo CancelOutFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
    
}