using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Infrastructure.Configuration;

namespace Taksaka.Infrastructure.Database;

public sealed class SqlServerConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() =>
        new SqlConnection(options.Value.ConnectionString);
}
