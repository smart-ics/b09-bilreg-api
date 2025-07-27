// using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
// using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
// using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
//
// namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.JaminanAgg;
//
// public class JaminanDto
// {
//     public string fs_kd_jaminan { get; set; }
//     public string fs_nm_jaminan { get; set; }
//     public string fs_alm1_jaminan { get; set; }
//     public string fs_alm2_jaminan { get; set; }
//     public string fs_kota_jaminan { get; set; }
//     public bool fb_aktif { get; set; }
//     public string fs_kd_cara_bayar_dk { get; set; }
//     public string fs_nm_cara_bayar_dk { get; set; }
//     public string fs_kd_grup_jaminan { get; set; }
//     public string fs_nm_grup_jaminan { get; set; }
//     public string fs_benefit_mou { get; set; }
//
//     public JaminanModel ToModel()
//     {
//         var caraBayarDk = new CaraBayarDkModel(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
//         var grupJaminan = new GrupJaminanModel(fs_kd_grup_jaminan, fs_nm_grup_jaminan, false, string.Empty);
//         var address = new AddressType(fs_alm1_jaminan, fs_alm2_jaminan, string.Empty, fs_kota_jaminan, string.Empty);
//         var jaminan = new JaminanModel(fs_kd_jaminan, fs_nm_jaminan, address, 
//             fb_aktif, caraBayarDk, grupJaminan, fs_benefit_mou);
//
//         return jaminan;
//     }
// }