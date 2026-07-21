using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class SqlServerClock : ISqlServerClock
{
    private readonly DatabaseOptions _opt;

    public SqlServerClock(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public DateTime GetDate()
    {
        const string sql = @"SELECT GETDATE() TglJam";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        using var cmd = new SqlCommand(sql, conn);
        using var dr = cmd.ExecuteReader();
        dr.Read();
        return Convert.ToDateTime(dr["TglJam"]);
    }
}
