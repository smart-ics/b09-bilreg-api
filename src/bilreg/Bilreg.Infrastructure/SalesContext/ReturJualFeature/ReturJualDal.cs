using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public interface IReturJualDal :
    IInsert<ReturJualDto>,
    IUpdate<ReturJualDto>,
    IDelete<IReturJualKey>,
    IGetData<ReturJualDto, IReturJualKey>,
    IListData<ReturJualDto, IRegKey>
{
}

public class ReturJualDal : IReturJualDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public ReturJualDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ReturJualDto model)
    {
        const string sql = """
            INSERT INTO tb_trs_rjual_umum (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fs_kd_dobill_umum, fs_kd_reg, fs_kd_layanan,
                fs_keterangan, fs_kd_tipe_jaminan, fs_kd_tipe_barang,
                fn_total_jual, fn_total_retur, fn_total_tax, fn_pembulatan, fn_grand_total,
                CRTUSR)
            VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fs_kd_dobill_umum, @fs_kd_reg, @fs_kd_layanan,
                @fs_keterangan, @fs_kd_tipe_jaminan, @fs_kd_tipe_barang,
                @fn_total_jual, @fn_total_retur, @fn_total_tax, @fn_pembulatan, @fn_grand_total,
                @CRTUSR)
            """;

        var dp = BuildWriteParams(model);
        dp.AddParam("@CRTUSR", ApiUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ReturJualDto model)
    {
        const string sql = """
            UPDATE tb_trs_rjual_umum
            SET
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_kd_dobill_umum = @fs_kd_dobill_umum,
                fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fs_keterangan = @fs_keterangan, 
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan, 
                fs_kd_tipe_barang = @fs_kd_tipe_barang,
                fn_total_jual = @fn_total_jual, 
                fn_total_retur = @fn_total_retur, 
                fn_total_tax = @fn_total_tax, 
                fn_pembulatan = @fn_pembulatan, 
                fn_grand_total = @fn_grand_total,
                UPDUSR = @UPDUSR
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = BuildWriteParams(model);
        dp.AddParam("@fd_tgl_void", model.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", model.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", model.UserVoidId, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", ApiUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IReturJualKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_rjual_umum
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.ReturJualId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ReturJualDto GetData(IReturJualKey key)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_trs = @PenjualanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", key.ReturJualId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ReturJualDto>(sql, dp);
    }

    public IEnumerable<ReturJualDto> ListData(IRegKey filter)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_reg = @RegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ReturJualDto>(sql, dp);
    }

    private static DynamicParameters BuildWriteParams(ReturJualDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.PenjualanId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglJam.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TglJam.ToString(DateFormatEnum.HMS), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dobill_umum", model.PenjualanId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Reason, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_barang", model.TipeBrgId, SqlDbType.VarChar);
        dp.AddParam("@fn_total_jual", model.SumSubTotalJual, SqlDbType.Decimal);
        dp.AddParam("@fn_total_retur", model.SumSubTotalRetur, SqlDbType.Decimal);
        dp.AddParam("@fn_total_tax", model.SumTax, SqlDbType.Decimal);
        dp.AddParam("@fn_pembulatan", model.Pembulatan, SqlDbType.Decimal);
        dp.AddParam("@fn_grand_total", model.GrandTotal, SqlDbType.Decimal);
        return dp;
    }

    private static string SelectClause() => """
        SELECT
        	aa.fs_kd_trs AS ReturJualId,
        	CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) AS TglJam,
        	aa.fs_kd_petugas AS UserId,
        	aa.fs_kd_dobill_umum AS PenjualanId,
        	aa.fs_kd_reg AS RegId,
        	aa.fs_kd_layanan AS LayananId,
        	aa.fs_keterangan + aa.fs_keterangan2 AS Reason, 
        	aa.fs_kd_tipe_jaminan AS TipeJaminanId,
        	aa.fs_kd_tipe_barang AS TipeBarangId,
        	aa.fn_total_jual AS SumSubTotalJual,
        	aa.fn_total_retur AS SumSubTotalRetur,
        	aa.fn_total_tax AS SumTax,
        	aa.fn_pembulatan AS Pembulatan,
        	aa.fn_grand_total AS GrandTotal,
        	aa.fd_tgl_void AS TglVoid,
        	aa.fs_jam_void AS JamVoid,
        	aa.fs_kd_petugas_void AS UserVoidId,
        	ISNULL(bb.fs_mr, '') AS PasienId,
        	ISNULL(cc.fs_nm_pasien, '') AS PasienName,
        	ISNULL(dd.fs_nm_layanan, '') AS LayananName,
        	ISNULL(ee.fs_nm_tipe_jaminan, '') AS TipeJaminanName,
        	ISNULL(ff.fs_nm_tipe_barang, '') AS TipeBarangName
        FROM
        	tb_trs_rjual_umum aa
        	LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
        	LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
        	LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
        	LEFT JOIN ta_tipe_jaminan ee ON aa.fs_kd_tipe_jaminan = ee.fs_kd_tipe_jaminan
        	LEFT JOIN tb_tipe_barang ff ON aa.fs_kd_tipe_barang = ff.fs_kd_tipe_barang
        """;
}
