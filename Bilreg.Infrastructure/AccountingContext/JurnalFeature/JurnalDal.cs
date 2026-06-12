//  TODO: jude-dev-trs-billing
// using Bilreg.Domain.AccountingContext.JurnalFeature;
// using Bilreg.Infrastructure.Shared.Helpers;
// using Dapper;
// using Microsoft.Extensions.Options;
// using Nuna.Lib.DataAccessHelper;
// using System.Data;
// using System.Data.SqlClient;
//
// namespace Bilreg.Infrastructure.AccountingContext.JurnalFeature;
//
// public interface IJurnalDal :
//     IInsert<JurnalDto>,
//     IUpdate<JurnalDto>,
//     IDelete<IJurnalKey>,
//     IGetData<JurnalDto, IJurnalKey>
// {
// }
//
// public class JurnalDal : IJurnalDal
// {
//     private readonly DatabaseOptions _opt;
//     public JurnalDal(IOptions<DatabaseOptions> opt)
//     {
//         _opt = opt.Value;
//     }
//
//     public void Insert(JurnalDto dto)
//     {
//         const string sql = """
//             INSERT INTO t_jurnal_hdr(
//                 fs_kd_jurnal, fd_tgl_jurnal, fs_jam_jurnal, fs_kd_petugas,
//                 fs_keterangan, fs_no_bukti1, fs_no_bukti2, fs_no_bukti3,
//                 fs_kd_reg, fs_kd_mr)
//             VALUES( 
//                 @fs_kd_jurnal, @fd_tgl_jurnal, @fs_jam_jurnal, @fs_kd_petugas,
//                 @fs_keterangan, @fs_no_bukti1, @fs_no_bukti2, @fs_no_bukti3,
//                 @fs_kd_reg, @fs_kd_mr)
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_jurnal", dto.fs_kd_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fd_tgl_jurnal", dto.fd_tgl_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fs_jam_jurnal", dto.fs_jam_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
//         dp.AddParam("@fs_keterangan", dto.fs_keterangan, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti1", dto.fs_no_bukti1, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti2", dto.fs_no_bukti2, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti3", dto.fs_no_bukti3, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_mr", dto.fs_kd_mr, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Update(JurnalDto dto)
//     {
//         const string sql = """
//             UPDATE
//                 t_jurnal_hdr
//             SET
//                 fd_tgl_jurnal = @fd_tgl_jurnal,
//                 fs_jam_jurnal = @fs_jam_jurnal,
//                 fs_kd_petugas = @fs_kd_petugas,
//                 fs_keterangan = @fs_keterangan,
//                 fs_no_bukti1 = @fs_no_bukti1,
//                 fs_no_bukti2 = @fs_no_bukti2,
//                 fs_no_bukti3 = @fs_no_bukti3,
//                 fs_kd_reg = @fs_kd_reg,
//                 fs_kd_mr = @fs_kd_mr
//             WHERE
//                 fs_kd_jurnal = @fs_kd_jurnal
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_jurnal", dto.fs_kd_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fd_tgl_jurnal", dto.fd_tgl_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fs_jam_jurnal", dto.fs_jam_jurnal, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
//         dp.AddParam("@fs_keterangan", dto.fs_keterangan, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti1", dto.fs_no_bukti1, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti2", dto.fs_no_bukti2, SqlDbType.VarChar);
//         dp.AddParam("@fs_no_bukti3", dto.fs_no_bukti3, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_mr", dto.fs_kd_mr, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Delete(IJurnalKey key)
//     {
//         const string sql = """
//             DELETE FROM
//                 t_jurnal_hdr
//             WHERE
//                 fs_kd_jurnal = @fs_kd_jurnal
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_jurnal", key.JurnalId, SqlDbType.VarChar);
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public JurnalDto GetData(IJurnalKey key)
//     {
//         const string sql = """
//             SELECT
//                 aa.fs_kd_jurnal, aa.fd_tgl_jurnal, aa.fs_jam_jurnal, aa.fs_kd_petugas,
//                 aa.fs_keterangan, aa.fs_no_bukti1, aa.fs_no_bukti2, aa.fs_no_bukti3,
//                 aa.fs_kd_reg, aa.fs_kd_mr,
//                 ISNULL(bb.fs_nm_pasien, '') AS fs_nm_pasien
//             FROM
//                 t_jurnal_hdr aa
//                 LEFT JOIN tc_mr bb ON aa.fs_kd_mr = bb.fs_mr
//             WHERE
//                 aa.fs_kd_jurnal = @fs_kd_jurnal
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_jurnal", key.JurnalId, SqlDbType.VarChar);
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.ReadSingle<JurnalDto>(sql, dp);
//         return result;
//     }
// }
