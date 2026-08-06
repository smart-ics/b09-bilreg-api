using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public interface IResepDal :
    IInsert<ResepDto>,
    IUpdate<ResepDto>,
    IDelete<IResepKey>,
    IGetData<ResepDto, IResepKey>,
    IListData<ResepDto, IRegKey>
{
}

public class ResepDal : IResepDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public ResepDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ResepDto model)
    {
        const string sql = """
            INSERT INTO ta_trs_kartu_periksa (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_tipe_barang,
                fs_kd_reg, fs_kd_layanan, fs_kd_petugas_medis, fs_kd_medis_resep,
                fn_tinggi_badan, fn_berat_badan, fn_lpb,
                fs_kd_urgenitas, CRTUSR,
                fn_iter_resep, fb_resep)
            VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas, @fs_kd_tipe_barang,
                @fs_kd_reg, @fs_kd_layanan, @fs_kd_petugas_medis, @fs_kd_medis_resep,
                @fn_tinggi_badan, @fn_berat_badan, @fn_lpb,
                @fs_kd_urgenitas, @CRTUSR,
                @fn_iter_resep, @fb_resep)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.ResepId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglJam.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TglJam.ToString(DateFormatEnum.HMS), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_barang", model.TipeBarangId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_medis", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis_resep", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fn_tinggi_badan", model.TinggiBadan, SqlDbType.Decimal);
        dp.AddParam("@fn_berat_badan", model.BeratBadan, SqlDbType.Decimal);
        dp.AddParam("@fn_lpb", model.Lpb, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_urgenitas", model.UrgenitasId, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", ApiUser, SqlDbType.VarChar);
        dp.AddParam("@fn_iter_resep", model.Iter, SqlDbType.Decimal);
        dp.AddParam("@fb_resep", 1, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);

        const string sql2 = """
            INSERT INTO ta_trs_kartu_periksa_resep (
                fs_kd_trs, fs_catatan_resep)
            VALUES (
                @fs_kd_trs, @fs_catatan_resep)
            """;

        var dp2 = new DynamicParameters();
        dp2.AddParam("@fs_kd_trs", model.ResepId, SqlDbType.VarChar);
        dp2.AddParam("@fs_catatan_resep", model.Description, SqlDbType.VarChar);
        conn.Execute(sql2, dp2);
    }

    public void Update(ResepDto model)
    {
        const string sql = """
            UPDATE ta_trs_kartu_periksa
            SET
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fs_kd_tipe_barang = @fs_kd_tipe_barang,
                fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_petugas_medis = @fs_kd_petugas_medis,
                fs_kd_medis_resep = @fs_kd_medis_resep,
                fn_tinggi_badan = @fn_tinggi_badan,
                fn_berat_badan = @fn_berat_badan,
                fn_lpb = @fn_lpb,
                fs_kd_urgenitas = @fs_kd_urgenitas,
                UPDUSR = @UPDUSR,
                fn_iter_resep = @fn_iter_resep,
                fb_resep = @fb_resep,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.ResepId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglJam.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TglJam.ToString(DateFormatEnum.HMS), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_barang", model.TipeBarangId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_medis", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis_resep", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fn_tinggi_badan", model.TinggiBadan, SqlDbType.Decimal);
        dp.AddParam("@fn_berat_badan", model.BeratBadan, SqlDbType.Decimal);
        dp.AddParam("@fn_lpb", model.Lpb, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_urgenitas", model.UrgenitasId, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", ApiUser, SqlDbType.VarChar);
        dp.AddParam("@fn_iter_resep", model.Iter, SqlDbType.Decimal);
        dp.AddParam("@fb_resep", 1, SqlDbType.Bit);
        dp.AddParam("@fd_tgl_void", model.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", model.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", model.UserVoidId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);

        const string sql2 = """
            UPDATE ta_trs_kartu_periksa_resep
            SET
                fs_catatan_resep = @fs_catatan_resep
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp2 = new DynamicParameters();
        dp2.AddParam("@fs_kd_trs", model.ResepId, SqlDbType.VarChar);
        dp2.AddParam("@fs_catatan_resep", model.Description, SqlDbType.VarChar);
        conn.Execute(sql2, dp2);
    }

    public void Delete(IResepKey key)
    {
        const string sqlSide = """
            DELETE FROM ta_trs_kartu_periksa_resep
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        const string sql = """
            DELETE FROM ta_trs_kartu_periksa
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.ResepId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlSide, dp);
        conn.Execute(sql, dp);
    }

    public ResepDto GetData(IResepKey key)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_trs = @ResepId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResepId", key.ResepId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ResepDto>(sql, dp);
    }

    public IEnumerable<ResepDto> ListData(IRegKey filter)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_reg = @RegId
                AND aa.fb_resep = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ResepDto>(sql, dp);
    }

    private static string SelectClause() => """
        SELECT
            aa.fs_kd_trs AS ResepId,
            CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) AS TglJam,
            aa.fs_kd_reg AS RegId,
            aa.fs_kd_layanan AS LayananId,
            aa.fs_kd_medis_resep AS DokterId,
            aa.fs_kd_urgenitas AS UrgenitasId,
            CAST(aa.fn_iter_resep AS INT) AS Iter,
            aa.fs_kd_petugas AS UserId,
            aa.fd_tgl_void AS TglVoid,
            aa.fs_jam_void AS JamVoid,
            aa.fs_kd_petugas_void AS UserVoidId,
            aa.fn_tinggi_badan AS TinggiBadan,
            aa.fn_berat_badan AS BeratBadan,
            aa.fn_lpb AS Lpb,
            aa.fs_kd_tipe_barang AS TipeBarangId,
            ISNULL(bb.fs_catatan_resep, '') AS Description,
            ISNULL(cc.fs_mr, '') AS PasienId,
            ISNULL(dd.fs_nm_pasien, '') AS PasienName,
            ISNULL(ee.fs_nm_layanan, '') AS LayananName,
            ISNULL(ff.fs_nm_peg, '') AS DokterName,
            ISNULL(gg.UrgenitasName, '') AS UrgenitasName,
            ISNULL(hh.fs_nm_tipe_barang, '') AS TipeBarangName
        FROM
            ta_trs_kartu_periksa aa
            LEFT JOIN ta_trs_kartu_periksa_resep bb ON aa.fs_kd_trs = bb.fs_kd_trs
            LEFT JOIN ta_registrasi cc ON aa.fs_kd_reg = cc.fs_kd_reg
            LEFT JOIN tc_mr dd ON cc.fs_mr = dd.fs_mr
            LEFT JOIN ta_layanan ee ON aa.fs_kd_layanan = ee.fs_kd_layanan
            LEFT JOIN td_peg ff ON aa.fs_kd_medis_resep = ff.fs_kd_peg
            LEFT JOIN FARPU_Urgenitas gg ON aa.fs_kd_urgenitas = gg.UrgenitasId
            LEFT JOIN tb_tipe_barang hh ON aa.fs_kd_tipe_barang = hh.fs_kd_tipe_barang
        """;
}
