using System.Data;

namespace Taksaka.Abstractions.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
