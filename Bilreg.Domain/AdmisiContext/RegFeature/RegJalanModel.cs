// using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
// using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
// using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
// using CommunityToolkit.Diagnostics;
//
// namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
//
// public class RegJalanModel : RegModel
// {
//     private readonly List<RegJalanLayananVo> _listLayanan = [];
//     
//     public IEnumerable<RegJalanLayananVo> ListLayanan => _listLayanan;
//
//     public void AddLayanan(RegJalanLayananVo layanan)
//     {
//         Guard.IsNotNull(layanan);
//         _listLayanan.Add(layanan);
//     }
//
//     public RegJalanModel(string regId) : base(regId)
//     {
//     }
//
//     public RegJalanModel(string regId, TglJamTrsType tglJamTrs, VoidFlagType voidFlag, RegPasienVo pasien, RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk) : base(regId, tglJamTrs, voidFlag, pasien, tipeJaminan, caraMasuk)
//     {
//     }
// }
//
// public record RegJalanLayananVo
// {
//     public string LayananId { get; }
//     public string LayananName { get; }
//     public string DokterId { get; }
//     public string DokterName { get; }
//     public string JamBerobat { get; }
//     public int NoAntrian { get; }
//
//     public RegJalanLayananVo(LayananModel layanan, PetugasMedisModel dokter,
//         int noAntrian)
//     {
//         Guard.IsNotNull(layanan);
//         Guard.IsNotNullOrEmpty(layanan.LayananId);
//         Guard.IsNotNullOrEmpty(layanan.LayananName);
//     }
// }