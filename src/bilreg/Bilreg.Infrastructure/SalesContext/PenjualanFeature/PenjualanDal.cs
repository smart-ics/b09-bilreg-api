using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public interface IPenjualanDal :
    IInsert<PenjualanDto>,
    IUpdate<PenjualanDto>,
    IDelete<IPenjualanKey>,
    IGetData<PenjualanDto, IPenjualanKey>,
    IListData<PenjualanDto, IRegKey>
{
}

public class PenjualanDal : IPenjualanDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public PenjualanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PenjualanDto model)
    {
        const string sql = """
            INSERT INTO tb_trs_dobill_umum (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fs_kd_resep, fs_kd_layanan_resep, fs_kd_petugas_medis, fs_kd_layanan,
                fs_kd_tipe_jaminan, fs_kd_tipe_barang,
                fs_kd_reg, fs_nm_pasien,
                fn_sum_sub_total, fn_sum_biaya, fn_sum_tax_rupiah, fn_sub_total,
                fn_diskon_lain, fn_biaya_lain, fn_grand_total, fn_pembulatan, fn_bulat,
                CRTUSR)
            VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fs_kd_resep, @fs_kd_layanan_resep, @fs_kd_petugas_medis, @fs_kd_layanan,
                @fs_kd_tipe_jaminan, @fs_kd_tipe_barang,
                @fs_kd_reg, @fs_nm_pasien,
                @fn_sum_sub_total, @fn_sum_biaya, @fn_sum_tax_rupiah, @fn_sub_total,
                @fn_diskon_lain, @fn_biaya_lain, @fn_grand_total, @fn_pembulatan, @fn_bulat,
                @CRTUSR)
            """;

        var dp = BuildWriteParams(model);
        dp.AddParam("@CRTUSR", ApiUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PenjualanDto model)
    {
        const string sql = """
            UPDATE tb_trs_dobill_umum
            SET
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_kd_resep = @fs_kd_resep,
                fs_kd_layanan_resep = @fs_kd_layanan_resep,
                fs_kd_petugas_medis = @fs_kd_petugas_medis,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fs_kd_tipe_barang = @fs_kd_tipe_barang,
                fs_kd_reg = @fs_kd_reg,
                fs_nm_pasien = @fs_nm_pasien,
                fn_sum_sub_total = @fn_sum_sub_total,
                fn_sum_biaya = @fn_sum_biaya,
                fn_sum_tax_rupiah = @fn_sum_tax_rupiah,
                fn_sub_total = @fn_sub_total,
                fn_diskon_lain = @fn_diskon_lain,
                fn_biaya_lain = @fn_biaya_lain,
                fn_grand_total = @fn_grand_total,
                fn_pembulatan = @fn_pembulatan,
                fn_bulat = @fn_bulat,
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

    public void Delete(IPenjualanKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_dobill_umum
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PenjualanDto GetData(IPenjualanKey key)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_trs = @PenjualanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", key.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PenjualanDto>(sql, dp);
    }

    public IEnumerable<PenjualanDto> ListData(IRegKey filter)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_reg = @RegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PenjualanDto>(sql, dp);
    }

    private static DynamicParameters BuildWriteParams(PenjualanDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.PenjualanId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglJam.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TglJam.ToString(DateFormatEnum.HMS), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_resep", model.ResepId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_resep", model.LayananResepId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_medis", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_barang", model.TipeBarangId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fn_sum_sub_total", model.SumSubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_sum_biaya", model.SumBiaya, SqlDbType.Decimal);
        dp.AddParam("@fn_sum_tax_rupiah", model.SumTax, SqlDbType.Decimal);
        dp.AddParam("@fn_sub_total", model.SubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon_lain", model.DiskonLain, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_lain", model.BiayaLain, SqlDbType.Decimal);
        dp.AddParam("@fn_grand_total", model.GrandTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_pembulatan", model.Pembulatan, SqlDbType.Decimal);
        dp.AddParam("@fn_bulat", model.Bulat, SqlDbType.Decimal);
        return dp;
    }

    private static string SelectClause() => """
        SELECT
            aa.fs_kd_trs AS PenjualanId,
            CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) AS TglJam,
            aa.fs_kd_petugas AS UserId,
            aa.fs_kd_resep AS ResepId,
            aa.fs_kd_reg AS RegId,
            aa.fs_kd_layanan AS LayananId,
            aa.fs_kd_layanan_resep AS LayananResepId,
            aa.fs_kd_petugas_medis AS DokterId,
            aa.fs_kd_tipe_jaminan AS TipeJaminanId,
            aa.fs_kd_tipe_barang AS TipeBarangId,
            aa.fn_sum_sub_total AS SumSubTotal,
            aa.fn_sum_biaya AS SumBiaya,
            aa.fn_sum_tax_rupiah AS SumTax,
            aa.fn_sub_total AS SubTotal,
            aa.fn_diskon_lain AS DiskonLain,
            aa.fn_biaya_lain AS BiayaLain,
            aa.fn_grand_total AS GrandTotal,
            aa.fn_pembulatan AS Pembulatan,
            aa.fn_bulat AS Bulat,
            aa.fd_tgl_void AS TglVoid,
            aa.fs_jam_void AS JamVoid,
            aa.fs_kd_petugas_void AS UserVoidId,
            ISNULL(aa.fs_nm_pasien, '') AS PasienName,
            ISNULL(bb.fs_mr, '') AS PasienId,
            ISNULL(cc.fs_nm_layanan, '') AS LayananName,
            ISNULL(dd.fs_nm_layanan, '') AS LayananResepName,
            ISNULL(ee.fs_nm_peg, '') AS DokterName,
            ISNULL(ff.fs_nm_tipe_jaminan, '') AS TipeJaminanName,
            ISNULL(gg.fs_nm_tipe_barang, '') AS TipeBarangName
        FROM
            tb_trs_dobill_umum aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan_resep = dd.fs_kd_layanan
            LEFT JOIN td_peg ee ON aa.fs_kd_petugas_medis = ee.fs_kd_peg
            LEFT JOIN ta_tipe_jaminan ff ON aa.fs_kd_tipe_jaminan = ff.fs_kd_tipe_jaminan
            LEFT JOIN tb_tipe_barang gg ON aa.fs_kd_tipe_barang = gg.fs_kd_tipe_barang
        """;
}
