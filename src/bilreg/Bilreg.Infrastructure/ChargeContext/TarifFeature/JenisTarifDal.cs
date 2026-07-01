using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface IJenisTarifDal :
    IInsert<JenisTarifDto>,
    IUpdate<JenisTarifDto>,
    IDelete<IJenisTarifKey>,
    IGetData<JenisTarifDto, IJenisTarifKey>,
    IListData<JenisTarifDto>
{
}

public class JenisTarifDal : IJenisTarifDal
{
    private readonly DatabaseOptions _opt;

    public JenisTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JenisTarifDto dto)
    {
        const string sql = """
            INSERT INTO ta_jenis_tarif(
                fs_kd_jenis_tarif, fs_nm_jenis_tarif, fs_urut)
            VALUES( 
                @fs_kd_jenis_tarif, @fs_nm_jenis_tarif, @fs_urut)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", dto.fs_kd_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", dto.fs_nm_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", dto.fs_urut, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JenisTarifDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_jenis_tarif
           SET
               fs_nm_jenis_tarif = @fs_nm_jenis_tarif,
               fs_urut = @fs_urut
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", dto.fs_kd_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", dto.fs_nm_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", dto.fs_urut, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJenisTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_jenis_tarif
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JenisTarifDto GetData(IJenisTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_jenis_tarif,
               fs_nm_jenis_tarif,
               fs_urut
           FROM 
               ta_jenis_tarif
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<JenisTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<JenisTarifDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_jenis_tarif,
                fs_nm_jenis_tarif,
                fs_urut
            FROM 
                ta_jenis_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisTarifDto>(sql);
    }
}
