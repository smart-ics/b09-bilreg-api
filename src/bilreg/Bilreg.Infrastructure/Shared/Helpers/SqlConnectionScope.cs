using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.Shared;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class SqlConnectionScope : ISqlConnectionScope
{
    private readonly DatabaseOptions _opt;
    private SqlConnection? _connection;
    private System.Transactions.Transaction? _enlistedTx;
    private readonly object _lock = new();

    public SqlConnectionScope(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public SqlConnection GetConnection()
    {
        lock (_lock)
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(ConnStringHelper.Get(_opt));
                _connection.Open();
            }
            else if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            var currentTx = System.Transactions.Transaction.Current;
            if (currentTx != null && _enlistedTx != currentTx)
            {
                _connection.EnlistTransaction(currentTx);
                _enlistedTx = currentTx;
            }
            else if (currentTx == null && _enlistedTx != null)
            {
                _connection.EnlistTransaction(null);
                _enlistedTx = null;
            }

            return _connection;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_connection != null)
            {
                try
                {
                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                }
                catch
                {
                    // Ignore disposal errors
                }
                finally
                {
                    _connection.Dispose();
                    _connection = null;
                    _enlistedTx = null;
                }
            }
        }
    }
}
