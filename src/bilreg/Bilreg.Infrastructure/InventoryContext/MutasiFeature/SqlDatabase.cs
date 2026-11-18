using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public class SqlDatabase : ISqlDatabase
{
    private readonly DatabaseOptions _opt;

    public SqlDatabase(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(ConnStringHelper.Get(_opt));
    }

    public void ExecuteTransaction(Action<SqlConnection, SqlTransaction> action)
    {
        using var conn = GetConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            action(conn, trans);
            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }
}
