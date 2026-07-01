using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Infrastructure.Configuration;

namespace Taksaka.Infrastructure.Database;

public sealed class SqliteConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() =>
        new SqliteConnection(options.Value.ConnectionString);
}
