using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace Farinv.Infrastructure.Helpers;

public class Sequencer : ISequencer
{
    private readonly DatabaseOptions _opt;

    public Sequencer(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void CreateSequence(string sequenceTag)
    {
        sequenceTag = $"sq_{sequenceTag.ToLower()}";
        const string sql = "CREATE SEQUENCE [{0}] START WITH 1 INCREMENT BY 1";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.ExecuteAsync(string.Format(sql, sequenceTag));    
    }

    public int GetNextNoUrut(string sequenceTag)
    {
        sequenceTag = $"sq_{sequenceTag.ToLower()}";

        var sql = $"""
           BEGIN TRY
               EXEC ('SELECT NEXT VALUE FOR [dbo].[{sequenceTag}]')
           END TRY
           BEGIN CATCH
               IF ERROR_NUMBER() IN (11716, 208) -- 11716 = sequence not found, 208 = invalid object name
               BEGIN
                   EXEC ('
                       CREATE SEQUENCE [dbo].[{sequenceTag}]
                       START WITH 1
                       INCREMENT BY 1;
                   ')
                   EXEC ('SELECT NEXT VALUE FOR [dbo].[{sequenceTag}]')
               END
               ELSE
                   THROW;
           END CATCH
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql);
    }
}