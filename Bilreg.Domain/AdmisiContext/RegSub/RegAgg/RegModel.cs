// using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
// using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
// using CommunityToolkit.Diagnostics;
//
// namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
//
// public partial class RegModel : IRegKey
// {
//     private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
//     private const string CARAMASUK_DATANGSENDIRI_ID = "8";
//     
//     public string RegId { get; private set; }
//     public JenisRegEnum JenisReg { get; private set; }
//     public TglJamTrsType TglJamTrs { get; private set; }
//     public VoidFlagType VoidFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
//     public PasienViewType Pasien { get; private set; }
//
//     public RegTipeJaminanVo TipeJaminan { get; private set; }
//     public RegCaraMasukVo CaraMasuk { get; private set; }
//     public KarcisTarifVo KarcisTarif { get; private set; }
//     public RegKelasVo Kelas { get; private set; }
//     
//     #region METHOD
//     public RegModel(string regId)
//     {
//         RegId = regId;
//     }
//
//     public RegModel(string regId, TglJamTrsType tglJamTrs,
//         VoidFlagType voidFlag, RegPasienVo pasien, 
//         RegTipeJaminanVo tipeJaminan, RegCaraMasukVo caraMasuk)
//     {
//         RegId = regId;
//         TglJamTrs = tglJamTrs;
//         VoidFlag = voidFlag;
//
//         Pasien = pasien;
//         TipeJaminan = tipeJaminan;
//         CaraMasuk = caraMasuk;
//     }
//
//     public void SetTglJamTrs(TglJamTrsType tglJamTrs)
//     {
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         TglJamTrs = tglJamTrs;
//     }
//
//
//     public void SetVoidFlag(VoidFlagType voidFlag)
//     {
//         if (voidFlag.VoidDate == DefaultDate)
//             throw new ArgumentException("VoidDate invalid");
//         VoidFlag = voidFlag;
//     }
//     
//     public void SetPasien(RegPasienVo pasien)
//     {
//         Guard.IsNotNull(pasien);
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         Pasien = pasien;
//     }
//     
//     public void SetJaminan(RegTipeJaminanVo tipeJaminan)
//     {
//         Guard.IsNotNull(tipeJaminan);
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         TipeJaminan = tipeJaminan;
//     }
//
//     public void SetCaraMasuk(RegCaraMasukVo caraMasuk)
//     {
//         Guard.IsNotNull(caraMasuk);
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         CaraMasuk = caraMasuk;
//     }
//     
//     public void SetKarcisTarif(KarcisTarifVo karcisTarif)
//     {
//         Guard.IsNotNull(karcisTarif);
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         KarcisTarif = karcisTarif;
//     }
//
//     public void SetKelas(RegKelasVo regKelas)
//     {
//         Guard.IsNotNull(regKelas);
//         if (VoidFlag.IsVoid)
//             throw new ArgumentException("Register sudah void");
//
//         Kelas = regKelas;            
//     }
//     #endregion    
// }