using System.Data.SqlClient;

namespace Bilreg.Application.Shared;

public interface ISqlConnectionScope : IDisposable
{
    SqlConnection GetConnection();
}
