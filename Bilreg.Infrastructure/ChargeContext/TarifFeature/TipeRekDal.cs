using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITipeRekDal :
    IInsert<TipeRekDto>,
    IUpdate<TipeRekDto>,
    IDelete<ITipeRekKey>,
    IGetData<TipeRekDto, ITipeRekKey>,
    IListData<TipeRekDto>
{
}

public class TipeRekDal : ITipeRekDal
{
    private readonly DatabaseOptions _opt;

    public TipeRekDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(TipeRekDto dto)
    {
        const string sql = """
            INSERT INTO t_rek_tipe(
                fs_kd_rek_tipe, fs_nm_rek_tipe)
            VALUES( 
                @fs_kd_rek_tipe, @fs_nm_rek_tipe)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rek_tipe", dto.fs_kd_rek_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rek_tipe", dto.fs_nm_rek_tipe, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeRekDto dto)
    {
        const string sql = @"
           UPDATE 
               t_rek_tipe
           SET
               fs_nm_rek_tipe = @fs_nm_rek_tipe
           WHERE
               fs_kd_rek_tipe = @fs_kd_rek_tipe";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rek_tipe", dto.fs_kd_rek_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rek_tipe", dto.fs_nm_rek_tipe, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeRekKey key)
    {
        const string sql = @"
           DELETE FROM 
                t_rek_tipe
           WHERE
               fs_kd_rek_tipe = @fs_kd_rek_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rek_tipe", key.TipeRekId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeRekDto GetData(ITipeRekKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_rek_tipe,
               fs_nm_rek_tipe
           FROM 
               t_rek_tipe
           WHERE
               fs_kd_rek_tipe = @fs_kd_rek_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rek_tipe", key.TipeRekId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<TipeRekDto>(sql, dp);
        return result;
    }

    public IEnumerable<TipeRekDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_rek_tipe,
                fs_nm_rek_tipe
            FROM 
                t_rek_tipe
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeRekDto>(sql);
    }
}
