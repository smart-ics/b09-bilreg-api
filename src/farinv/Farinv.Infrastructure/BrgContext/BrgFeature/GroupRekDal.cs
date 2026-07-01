using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IGroupRekDal :
    IInsert<GroupRekDto>,
    IUpdate<GroupRekDto>,
    IDelete<IGroupRekKey>,
    IGetData<GroupRekDto, IGroupRekKey>,
    IListData<GroupRekDto>
{
}

public class GroupRekDal : IGroupRekDal
{
    private readonly DatabaseOptions _opt;
    public GroupRekDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupRekDto dto)
    {
        const string sql = """
            INSERT INTO tb_grup_rek(
                fs_kd_grup_rek, fs_nm_grup_rek, fs_kd_grup_rek_dk)
            VALUES( 
                @fs_kd_grup_rek, @fs_nm_grup_rek, @fs_kd_grup_rek_dk)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rek", dto.fs_nm_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek_dk", dto.fs_kd_grup_rek_dk, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(GroupRekDto dto)
    {
        const string sql = """
           UPDATE 
               tb_grup_rek
           SET
               fs_nm_grup_rek = @fs_nm_grup_rek,
               fs_kd_grup_rek_dk = @fs_kd_grup_rek_dk
           WHERE
               fs_kd_grup_rek = @fs_kd_grup_rek
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek", dto.fs_kd_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_rek", dto.fs_nm_grup_rek, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_rek_dk", dto.fs_kd_grup_rek_dk, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IGroupRekKey key)
    {
        const string sql = """
           DELETE FROM 
                tb_grup_rek
           WHERE
               fs_kd_grup_rek = @fs_kd_grup_rek
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek", key.GroupRekId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public GroupRekDto GetData(IGroupRekKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_grup_rek,
               aa.fs_nm_grup_rek,
               aa.fs_kd_grup_rek_dk,
               ISNULL(bb.fs_nm_grup_rek_dk, '') fs_nm_grup_rek_dk
           FROM 
               tb_grup_rek aa
               LEFT JOIN tb_grup_rek_dk bb ON aa.fs_kd_grup_rek_dk = bb.fs_kd_grup_rek_dk
           WHERE
               aa.fs_kd_grup_rek = @fs_kd_grup_rek
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_rek", key.GroupRekId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupRekDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<GroupRekDto> ListData()
    {
        const string sql = """
            SELECT
                aa.fs_kd_grup_rek,
                aa.fs_nm_grup_rek,
                aa.fs_kd_grup_rek_dk,
                ISNULL(bb.fs_nm_grup_rek_dk, '') fs_nm_grup_rek_dk
            FROM 
                tb_grup_rek aa
                LEFT JOIN tb_grup_rek_dk bb ON aa.fs_kd_grup_rek_dk = bb.fs_kd_grup_rek_dk
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupRekDto>(sql);
    }
}