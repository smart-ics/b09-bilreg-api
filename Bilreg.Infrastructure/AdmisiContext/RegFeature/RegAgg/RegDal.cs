// using Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;
// using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
// using Bilreg.Infrastructure.Helpers;
// using Microsoft.Extensions.Options;
// using Nuna.Lib.ValidationHelper;
//
// namespace Bilreg.Infrastructure.AdmisiContext.RegSub.RegAgg;
//
// public class RegDal : IRegDal
// {
//     private readonly DatabaseOptions _opt;
//
//     public RegDal(IOptions<DatabaseOptions> opt)
//     {
//         _opt = opt.Value;
//     }
//
//     public void Insert(RegModel model)
//     {
//         const string sql = @"
//             INSERT INTO ta_registrasi (
//                 fs_kd_reg, fd_tgl_masuk, fs_jam_masuk, fs_kd_petugas,
//                 fd_tgl_void, fs_jam_void, fs_kd_petugas_void, 
//                 fd_tgl_keluar, fs_jam_keluar, fs_kd_petugas_keluar,
//                 fd_tgl_cancel_out, fs_jam_cancel_out, fs_kd_petugas_cancel_out,
//                 fs_mr, fs_kd_kelas, 
//                 fs_kd_tipe_jaminan, fs_no_peserta, fs_kd_status_peserta,
//                 fs_kd_cara_masuk_dk, fs_kd_rujukan, 
//             )";
//         
//         
//     }
//
//     public void Update(RegModel model)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void Delete(RegModel key)
//     {
//         throw new NotImplementedException();
//     }
//
//     public RegModel GetData(IRegKey key)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IEnumerable<RegModel> ListData(Periode filter)
//     {
//         throw new NotImplementedException();
//     }
// }