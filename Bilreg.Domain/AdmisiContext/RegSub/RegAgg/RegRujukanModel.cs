using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegRujukanModel : RegModel
{
    public RegRujukanModel(string regId) : base(regId)
    {
    }

    public RegRujukanModel(string regId, TglJamTrsType tglJamTrs, VoidFlagType voidFlag, 
        RegPasienVo pasien, RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk) : base(regId, tglJamTrs, voidFlag, pasien, tipeJaminan, caraMasuk)
    {
    }
    
    public LampiranRujukanVo LampiranRujukan { get; private set; }
}