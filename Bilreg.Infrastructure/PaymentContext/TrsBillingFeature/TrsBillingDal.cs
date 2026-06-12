//  TODO: jude-dev-trs-billing
// using System.Data;
// using System.Data.SqlClient;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.PaymentContext.TrsBillingFeature;
// using Bilreg.Infrastructure.Shared.Helpers;
// using Dapper;
// using Microsoft.Extensions.Options;
// using Nuna.Lib.DataAccessHelper;
//
// namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
//
// public interface ITrsBillingDal :
//     IInsert<TrsBillingDto>,
//     IUpdate<TrsBillingDto>,
//     IDelete<ITrsBillingKey>,
//     IGetData<TrsBillingDto, ITrsBillingKey>,
//     IListData<TrsBillingDto, IRegKey>
// {
// }
//
// public class TrsBillingDal : ITrsBillingDal
// {
//     private readonly DatabaseOptions _opt;
//     public TrsBillingDal(IOptions<DatabaseOptions> opt)
//     {
//         _opt = opt.Value;
//     }
//     
//     public void Insert(TrsBillingDto dto)
//     {
//         const string sql = """
//             INSERT INTO ta_trs_billing(
//                 fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs,
//                 fs_kd_reg, fs_kd_layanan, fs_kd_kelas, fs_kd_petugas,
//                 fn_sub_total, fn_diskon, fn_biaya, fn_tax, fn_total,
//                 fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak,
//                 fs_kd_ref_biaya, fn_qty, fs_kd_trs_main, fd_tgl_jam_trs)
//             VALUES( 
//                 @fs_kd_trs, @fn_modul, @fd_tgl_trs, @fs_jam_trs,
//                 @fs_kd_reg, @fs_kd_layanan, @fs_kd_kelas, @fs_kd_petugas,
//                 @fn_sub_total, @fn_diskon, @fn_biaya, @fn_tax, @fn_total,
//                 @fs_keterangan, @fs_keterangan2, @fs_kd_rekap_cetak,
//                 @fs_kd_ref_biaya, @fn_qty, @fs_kd_trs_main, @fd_tgl_jam_trs)
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
//         dp.AddParam("@fn_modul", dto.fn_modul, SqlDbType.Decimal);
//         dp.AddParam("@fd_tgl_trs", dto.fd_tgl_trs, SqlDbType.VarChar);
//         dp.AddParam("@fs_jam_trs", dto.fs_jam_trs, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
//         dp.AddParam("@fn_sub_total", dto.fn_sub_total, SqlDbType.Decimal);
//         dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);
//         dp.AddParam("@fn_biaya", dto.fn_biaya, SqlDbType.Decimal);
//         dp.AddParam("@fn_tax", dto.fn_tax, SqlDbType.Decimal);
//         dp.AddParam("@fn_total", dto.fn_total, SqlDbType.Decimal);
//         dp.AddParam("@fs_keterangan", dto.fs_keterangan, SqlDbType.VarChar);
//         dp.AddParam("@fs_keterangan2", dto.fs_keterangan2, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_rekap_cetak", dto.fs_kd_rekap_cetak, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_ref_biaya", dto.fs_kd_ref_biaya, SqlDbType.VarChar);
//         dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Int);
//         dp.AddParam("@fs_kd_trs_main", dto.fs_kd_trs_main, SqlDbType.VarChar);
//         dp.AddParam("@fd_tgl_jam_trs", dto.fd_tgl_jam_trs, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//     
//     public void Update(TrsBillingDto dto)
//     {
//         const string sql = """
//             UPDATE
//                 ta_trs_billing
//             SET
//                 fn_modul = @fn_modul,
//                 fd_tgl_trs = @fd_tgl_trs,
//                 fs_jam_trs = @fs_jam_trs,
//                 fs_kd_reg = @fs_kd_reg,
//                 fs_kd_layanan = @fs_kd_layanan,
//                 fs_kd_kelas = @fs_kd_kelas,
//                 fs_kd_petugas = @fs_kd_petugas,
//                 fn_sub_total = @fn_sub_total,
//                 fn_diskon = @fn_diskon,
//                 fn_biaya = @fn_biaya,
//                 fn_tax = @fn_tax,
//                 fn_total = @fn_total,
//                 fs_keterangan = @fs_keterangan,
//                 fs_keterangan2 = @fs_keterangan2,
//                 fs_kd_rekap_cetak = @fs_kd_rekap_cetak,
//                 fs_kd_ref_biaya = @fs_kd_ref_biaya,
//                 fn_qty = @fn_qty,
//                 fs_kd_trs_main = @fs_kd_trs_main,
//                 fd_tgl_jam_trs = @fd_tgl_jam_trs
//             WHERE
//                 fs_kd_trs = @fs_kd_trs
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
//         dp.AddParam("@fn_modul", dto.fn_modul, SqlDbType.Int);
//         dp.AddParam("@fd_tgl_trs", dto.fd_tgl_trs, SqlDbType.VarChar);
//         dp.AddParam("@fs_jam_trs", dto.fs_jam_trs, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
//         dp.AddParam("@fn_sub_total", dto.fn_sub_total, SqlDbType.Decimal);
//         dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);
//         dp.AddParam("@fn_biaya", dto.fn_biaya, SqlDbType.Decimal);
//         dp.AddParam("@fn_tax", dto.fn_tax, SqlDbType.Decimal);
//         dp.AddParam("@fn_total", dto.fn_total, SqlDbType.Decimal);
//         dp.AddParam("@fs_keterangan", dto.fs_keterangan, SqlDbType.VarChar);
//         dp.AddParam("@fs_keterangan2", dto.fs_keterangan2, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_rekap_cetak", dto.fs_kd_rekap_cetak, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_ref_biaya", dto.fs_kd_ref_biaya, SqlDbType.VarChar);
//         dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Int);
//         dp.AddParam("@fs_kd_trs_main", dto.fs_kd_trs_main, SqlDbType.VarChar);
//         dp.AddParam("@fd_tgl_jam_trs", dto.fd_tgl_jam_trs, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//     
//     public void Delete(ITrsBillingKey key)
//     {
//         const string sql = """
//             DELETE FROM
//                 ta_trs_billing
//             WHERE
//                 fs_kd_trs = @fs_kd_trs
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_trs", key.TrsBillingId, SqlDbType.VarChar);
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//     
//     public TrsBillingDto GetData(ITrsBillingKey key)
//     {
//         const string sql = """
//             SELECT
//                 aa.fs_kd_trs, aa.fn_modul, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fd_tgl_jam_trs,
//                 aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_kd_kelas, 
//                 aa.fs_kd_rekap_cetak, aa.fs_kd_petugas, 
//                 aa.fn_sub_total, aa.fn_diskon, aa.fn_biaya, aa.fn_tax, aa.fn_total,
//                 aa.fs_keterangan, aa.fs_keterangan2, aa.fs_kd_ref_biaya,
//                 aa.fn_qty, aa.fs_kd_trs_main,
//                 ISNULL(bb.fs_mr, '') AS fs_mr, 
//                 ISNULL(cc.fs_nm_pasien, '') AS fs_nm_pasien, 
//                 ISNULL(dd.fs_nm_layanan, '') AS fs_nm_layanan, 
//                 ISNULL(ee.fs_nm_kelas, '') AS fs_nm_kelas, 
//                 ISNULL(ff.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak 
//             FROM
//                 ta_trs_billing aa
//                 LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
//                 LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
//                 LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
//                 LEFT JOIN ta_kelas ee ON aa.fs_kd_kelas = ee.fs_kd_kelas
//                 LEFT JOIN ta_rekap_cetak_tarif ff ON aa.fs_kd_rekap_cetak = ff.fs_kd_rekap_cetak_tarif
//             WHERE
//                 aa.fs_kd_trs = @fs_kd_trs
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_trs", key.TrsBillingId, SqlDbType.VarChar);
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.ReadSingle<TrsBillingDto>(sql, dp);
//         return result;
//     }
//     
//     public IEnumerable<TrsBillingDto> ListData(IRegKey regKey)
//     {
//         const string sql = """
//             SELECT
//                 aa.fs_kd_trs, aa.fn_modul, aa.fd_tgl_trs, aa.fs_jam_trs, aa.fd_tgl_jam_trs,
//                 aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_kd_kelas, 
//                 aa.fs_kd_rekap_cetak, aa.fs_kd_petugas, 
//                 aa.fn_sub_total, aa.fn_diskon, aa.fn_biaya, aa.fn_tax, aa.fn_total,
//                 aa.fs_keterangan, aa.fs_keterangan2, aa.fs_kd_ref_biaya,
//                 aa.fn_qty, aa.fs_kd_trs_main,
//                 ISNULL(bb.fs_mr, '') AS fs_mr, 
//                 ISNULL(cc.fs_nm_pasien, '') AS fs_nm_pasien, 
//                 ISNULL(dd.fs_nm_layanan, '') AS fs_nm_layanan, 
//                 ISNULL(ee.fs_nm_kelas, '') AS fs_nm_kelas, 
//                 ISNULL(ff.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak 
//             FROM
//                 ta_trs_billing aa
//                 LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
//                 LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
//                 LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
//                 LEFT JOIN ta_kelas ee ON aa.fs_kd_kelas = ee.fs_kd_kelas
//                 LEFT JOIN ta_rekap_cetak_tarif ff ON aa.fs_kd_rekap_cetak = ff.fs_kd_rekap_cetak_tarif
//             WHERE
//                 aa.fs_kd_reg = @fs_kd_reg
//             
//             """;
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_reg", regKey.RegId, SqlDbType.VarChar);
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         return conn.Read<TrsBillingDto>(sql, dp);
//     }
// }