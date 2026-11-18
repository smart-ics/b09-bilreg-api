using System.Data.SqlClient;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface ISqlDatabase
{
    SqlConnection GetConnection();
    void ExecuteTransaction(Action<SqlConnection, SqlTransaction> action);
}
