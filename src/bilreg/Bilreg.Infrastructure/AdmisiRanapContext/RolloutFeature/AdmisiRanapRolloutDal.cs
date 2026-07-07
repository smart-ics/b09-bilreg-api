using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.RolloutFeature;

public class AdmisiRanapRolloutDal : IAdmisiRanapRolloutDal
{
    private readonly DatabaseOptions _opt;

    public AdmisiRanapRolloutDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public bool TableExists(string tableName)
    {
        const string sql = """
            SELECT 1
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = @TableName
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TableName", tableName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<int>(sql, dp).Any();
    }
}
