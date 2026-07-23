using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueRolloutDal : IAdmissionQueueRolloutDal
{
    private readonly DatabaseOptions _opt;

    public AdmissionQueueRolloutDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

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

    public bool IndexExists(string indexName, string tableName)
    {
        const string sql = """
            SELECT 1
            FROM sys.indexes
            WHERE name = @IndexName
              AND object_id = OBJECT_ID(@TableName)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IndexName", indexName, SqlDbType.VarChar);
        dp.AddParam("@TableName", tableName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<int>(sql, dp).Any();
    }
}
