using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public interface IPartnerDal:
    IInsert<PartnerDto>,
    IUpdate<PartnerDto>,
    IDelete<IPartnerKey>,
    IGetData<PartnerDto, IPartnerKey>,
    IListData<PartnerDto>;

public class PartnerDal: IPartnerDal
{
    private readonly DatabaseOptions _opt;

    public PartnerDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PartnerDto model)
    {
        const string sql = """
            INSERT INTO t_iii (
                fs_kd_iii,
                fs_nm_iii
            VALUES (
                @fs_kd_iii,
                @fs_nm_iii)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_iii", model.PartnerId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_iii", model.PartnerName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PartnerDto model)
    {
        const string sql = """
           UPDATE 
               t_iii
           SET
               fs_nm_iii = @fs_nm_iii
           WHERE
               fs_kd_iii = @fs_kd_iii
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_iii", model.PartnerId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_iii", model.PartnerName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPartnerKey key)
    {
        const string sql = """
           DELETE FROM 
               t_iii
           WHERE
               fs_kd_iii = @fs_kd_iii
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_iii", key.PartnerId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PartnerDto GetData(IPartnerKey key)
    {
        var sql = $"""
           {SelectClause()}
           WHERE
                aa.fs_kd_iii = @PartnerId
           AND
                aa.fb_aktif = '1'
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@PartnerId", key.PartnerId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PartnerDto>(sql, dp);
    }

    public IEnumerable<PartnerDto> ListData()
    {
        var sql = $"""
           {SelectClause()}
           WHERE
                aa.fb_aktif = '1'
           """;

        var dp = new DynamicParameters();
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PartnerDto>(sql, dp);
    }
    
    private static string SelectClause() => """
        SELECT
            aa.fs_kd_iii AS PartnerId,
            aa.fs_nm_iii AS PartnerName
        FROM
            t_iii aa
        """;
}