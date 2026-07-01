using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public interface IJkDal :
    IGetData<JkDto, IJkKey>,
    IListData<JkDto>
{ }
public class JkDal : IJkDal
{
    private readonly DatabaseOptions _opt;
    public JkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public JkDto GetData(IJkKey key)
    {
        const string sql = @"
                SELECT
                    fs_kd_jk, fs_nm_jk, 
                    fn_urut
                 FROM 
                    t_jk
                 WHERE
                    fs_kd_jk = @fs_kd_jk
                 ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jk", key.JkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JkDto>(sql, dp);
    }

    public IEnumerable<JkDto> ListData()
    {
        const string sql = @"
                SELECT
                    fs_kd_jk, fs_nm_jk, 
                    fn_urut
                 FROM 
                    t_jk
                 ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JkDto>(sql);
    }
}

public record JkDto(
    string fs_kd_jk,
    string fs_nm_jk,
    int fn_urut)
{
    public JkType ToModel()
    {
        return new JkType(
            fs_kd_jk,
            fs_nm_jk,
            (int)fn_urut);
    }
}