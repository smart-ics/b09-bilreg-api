using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Infrastructure.Shared.Helpers;


//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub;

public interface IRujukanDal :
    IInsert<RujukanDto>,
    IUpdate<RujukanDto>,
    IDelete<IRujukanKey>,
    IGetData<RujukanDto, IRujukanKey>,
    IListData<RujukanDto, ITipeRujukanKey>,
    IListData<RujukanDto, ICaraMasukDkKey>
{
}

public class RujukanDal : IRujukanDal
{
    private readonly DatabaseOptions _opt;

    public RujukanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    
    public void Insert(RujukanDto model)
    {
        const string sql = """
            INSERT INTO ta_rujukan (
                fs_kd_rujukan, fs_nm_rujukan, fb_aktif, fs_alm_rujukan, 
                fs_alm2_rujukan, fs_kota_rujukan, fs_tlp_rujukan, 
                fs_kd_rujukan_tipe, fs_kd_kelas_rs, fs_kd_cara_masuk_dk)
            VALUES (
                @fs_kd_rujukan, @fs_nm_rujukan, @fb_aktif, @fs_alm_rujukan, 
                @fs_alm2_rujukan, @fs_kota_rujukan, @fs_tlp_rujukan, 
                @fs_kd_rujukan_tipe, @fs_kd_kelas_rs,@fs_kd_cara_masuk_dk)
            """;
    
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", model.fs_kd_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan", model.fs_nm_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_alm_rujukan", model.fs_alm_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_rujukan", model.fs_alm2_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_rujukan", model.fs_kota_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_rujukan", model.fs_tlp_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rujukan_tipe", model.fs_kd_rujukan_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas_rs", model.fs_kd_kelas_rs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", model.fs_kd_cara_masuk_dk, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(RujukanDto model)
    {
        const string sql = """
            UPDATE ta_rujukan
            SET 
                fs_nm_rujukan = @fs_nm_rujukan,
                fb_aktif = @fb_aktif,
                fs_alm_rujukan = @fs_alm_rujukan,
                fs_alm2_rujukan = @fs_alm2_rujukan,
                fs_kota_rujukan = @fs_kota_rujukan,
                fs_tlp_rujukan = @fs_tlp_rujukan,
                fs_kd_rujukan_tipe = @fs_kd_rujukan_tipe,
                fs_kd_kelas_rs = @fs_kd_kelas_rs,
                fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk
            WHERE 
                fs_kd_rujukan = @fs_kd_rujukan
            """;
    
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", model.fs_kd_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan", model.fs_nm_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_alm_rujukan", model.fs_alm_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_rujukan", model.fs_alm2_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_rujukan", model.fs_kota_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_rujukan", model.fs_tlp_rujukan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rujukan_tipe", model.fs_kd_rujukan_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas_rs", model.fs_kd_kelas_rs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_masuk_dk", model.fs_kd_cara_masuk_dk, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IRujukanKey key)
    {
        const string sql = """
           DELETE FROM
                ta_rujukan
           WHERE 
               fs_kd_rujukan = @fs_kd_rujukan
           """;
    
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", key.RujukanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public RujukanDto GetData(IRujukanKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_rujukan, aa.fs_nm_rujukan, aa.fs_alm_rujukan,
                aa.fs_alm2_rujukan, aa.fs_kota_rujukan, aa.fs_tlp_rujukan,
                aa.fs_kd_rujukan_tipe, aa.fs_kd_kelas_rs, aa.fs_kd_cara_masuk_dk,
                aa.fb_aktif, 
                ISNULL(bb.fs_nm_rujukan_tipe, '') fs_nm_rujukan_tipe,
                ISNULL(cc.fs_nm_kelas_rs, '') fs_nm_kelas_rs,
                ISNULL(dd.fs_nm_cara_masuk_dk, '') fs_nm_cara_masuk_dk
            FROM ta_rujukan aa
                LEFT JOIN ta_rujukan_tipe bb ON aa.fs_kd_rujukan_tipe = bb.fs_kd_rujukan_tipe
                LEFT JOIN tc_kelas_rs cc ON aa.fs_kd_kelas_rs = cc.fs_kd_kelas_rs
                LEFT JOIN ta_cara_masuk_dk dd ON aa.fs_kd_cara_masuk_dk = dd.fs_kd_cara_masuk_dk
            WHERE 
                aa.fs_kd_rujukan = @fs_kd_rujukan
            """;
    
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan", key.RujukanId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<RujukanDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<RujukanDto> ListData(ITipeRujukanKey tipeRujukan)
    {
        const string sql = """
            SELECT
                aa.fs_kd_rujukan, aa.fs_nm_rujukan, aa.fs_alm_rujukan,
                aa.fs_alm2_rujukan, aa.fs_kota_rujukan, aa.fs_tlp_rujukan,
                aa.fs_kd_rujukan_tipe, aa.fs_kd_kelas_rs, aa.fs_kd_cara_masuk_dk,
                aa.fb_aktif, 
                ISNULL(bb.fs_nm_rujukan_tipe, '') fs_nm_rujukan_tipe,
                ISNULL(cc.fs_nm_kelas_rs, '') fs_nm_kelas_rs,
                ISNULL(dd.fs_nm_cara_masuk_dk, '') fs_nm_cara_masuk_dk
            FROM ta_rujukan aa
                LEFT JOIN ta_rujukan_tipe bb ON aa.fs_kd_rujukan_tipe = bb.fs_kd_rujukan_tipe
                LEFT JOIN tc_kelas_rs cc ON aa.fs_kd_kelas_rs = cc.fs_kd_kelas_rs
                LEFT JOIN ta_cara_masuk_dk dd ON aa.fs_kd_cara_masuk_dk = dd.fs_kd_cara_masuk_dk
            WHERE
                aa.fs_kd_rujukan_tipe = @TipeRujukanId
                AND aa.fb_aktif = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TipeRujukanId", tipeRujukan.TipeRujukanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<RujukanDto>(sql, dp);
        return result;
    }

    public IEnumerable<RujukanDto> ListData(ICaraMasukDkKey caraMasukDkKey)
    {
        const string sql = """
            SELECT
                aa.fs_kd_rujukan, aa.fs_nm_rujukan, aa.fs_alm_rujukan,
                aa.fs_alm2_rujukan, aa.fs_kota_rujukan, aa.fs_tlp_rujukan,
                aa.fs_kd_rujukan_tipe, aa.fs_kd_kelas_rs, aa.fs_kd_cara_masuk_dk,
                aa.fb_aktif, 
                ISNULL(bb.fs_nm_rujukan_tipe, '') fs_nm_rujukan_tipe,
                ISNULL(cc.fs_nm_kelas_rs, '') fs_nm_kelas_rs,
                ISNULL(dd.fs_nm_cara_masuk_dk, '') fs_nm_cara_masuk_dk
            FROM ta_rujukan aa
                LEFT JOIN ta_rujukan_tipe bb ON aa.fs_kd_rujukan_tipe = bb.fs_kd_rujukan_tipe
                LEFT JOIN tc_kelas_rs cc ON aa.fs_kd_kelas_rs = cc.fs_kd_kelas_rs
                LEFT JOIN ta_cara_masuk_dk dd ON aa.fs_kd_cara_masuk_dk = dd.fs_kd_cara_masuk_dk
            WHERE
                aa.fs_kd_cara_masuk_dk = @CaraMasukDkId
                AND aa.fb_aktif = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@CaraMasukDkId", caraMasukDkKey.CaraMasukDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<RujukanDto>(sql, dp);
        return result;
    }
}