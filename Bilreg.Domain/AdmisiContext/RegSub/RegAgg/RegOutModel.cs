// using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
//
// namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
//
// public class RegOutModel : RegModel
// {
//     public RegOutModel(string regId) : base(regId)
//     {
//     }
//
//     public RegOutModel(string regId, TglJamTrsType tglJamTrs, VoidFlagType voidFlag, RegPasienVo pasien, 
//         RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk) : base(regId, tglJamTrs, voidFlag, pasien, tipeJaminan, caraMasuk)
//     {
//     }
//     
//     public VoidFlagType RegOutFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
//     public VoidFlagType CancelOutFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
//     
// }