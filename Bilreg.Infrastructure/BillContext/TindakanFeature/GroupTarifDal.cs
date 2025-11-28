using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BillContext.TindakanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public interface IGroupTarifDal :
    IInsert<GroupTarifDto>,
    IUpdate<GroupTarifDto>,
    IDelete<IGroupTarifKey>,
    IGetData<GroupTarifDto, IGroupTarifKey>,
    IListData<GroupTarifDto>
{
}

public class GroupTarifDal : IGroupTarifDal
{
    private readonly DatabaseOptions _opt;

    public GroupTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupTarifDto dto)
    {
        const string sql = """
            INSERT INTO ta_grup_tarif(
                fs_kd_grup_tarif, fs_nm_grup_tarif)
            VALUES( 
                @fs_kd_grup_tarif, @fs_nm_grup_tarif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", dto.fs_kd_grup_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", dto.fs_nm_grup_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupTarifDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_grup_tarif
           SET
               fs_nm_grup_tarif = @fs_nm_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", dto.fs_kd_grup_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", dto.fs_nm_grup_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GroupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupTarifDto GetData(IGroupTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_grup_tarif,
               fs_nm_grup_tarif
           FROM 
               ta_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GroupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<GroupTarifDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_grup_tarif,
                fs_nm_grup_tarif
            FROM 
                ta_grup_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupTarifDto>(sql);
    }
}
