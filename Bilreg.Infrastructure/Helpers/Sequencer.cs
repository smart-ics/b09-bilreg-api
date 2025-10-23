using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.Helpers;

public class Sequencer : ISequencer
{
    private readonly DatabaseOptions _opt;

    public Sequencer(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void CreateSequence(string sequenceTag)
    {
        const string sql = "CREATE SEQUENCE [{0}] START WITH 1 INCREMENT BY 1";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.ExecuteAsync(string.Format(sql, sequenceTag));    
    }

    public int GetNextNoUrut(string sequenceTag)
    {
        var sql = $"SELECT NEXT VALUE FOR [{sequenceTag}]";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql);
    }
}