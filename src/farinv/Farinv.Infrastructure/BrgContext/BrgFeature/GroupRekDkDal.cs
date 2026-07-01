using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IGroupRekDkDal :
    IInsert<GroupRekDkDto>,
    IUpdate<GroupRekDkDto>,
    IDelete<IGroupRekDkKey>,
    IGetData<GroupRekDkDto, IGroupRekDkKey>,
    IListData<GroupRekDkDto>
{
}

public class GroupRekDkDal : IGroupRekDkDal
{
    private readonly DatabaseOptions _opt;
    public GroupRekDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupRekDkDto dto)
    {
        const string sql = """
            INSERT INTO tb_grup_rek_dk(
                fs_kd_grup_rek_dk, fs_nm_grup_rek_dk)
            VALUES( 
                @fs_kd_grup_rek_dk, @fs_nm_grup_rek_dk)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek_dk", dto.fs_kd_grup_rek_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rek_dk", dto.fs_nm_grup_rek_dk, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(GroupRekDkDto dto)
    {
        const string sql = """
           UPDATE 
               tb_grup_rek_dk
           SET
               fs_nm_grup_rek_dk = @fs_nm_grup_rek_dk
           WHERE
               fs_kd_grup_rek_dk = @fs_kd_grup_rek_dk
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek_dk", dto.fs_kd_grup_rek_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rek_dk", dto.fs_nm_grup_rek_dk, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IGroupRekDkKey key)
    {
        const string sql = """
           DELETE FROM 
                tb_grup_rek_dk
           WHERE
               fs_kd_grup_rek_dk = @fs_kd_grup_rek_dk
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek_dk", key.GroupRekDkId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public GroupRekDkDto GetData(IGroupRekDkKey key)
    {
        const string sql = """
           SELECT
               fs_kd_grup_rek_dk,
               fs_nm_grup_rek_dk
           FROM 
               tb_grup_rek_dk
           WHERE
               fs_kd_grup_rek_dk = @fs_kd_grup_rek_dk
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek_dk", key.GroupRekDkId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupRekDkDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<GroupRekDkDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_grup_rek_dk,
                fs_nm_grup_rek_dk
            FROM 
                tb_grup_rek_dk
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupRekDkDto>(sql);
    }
}