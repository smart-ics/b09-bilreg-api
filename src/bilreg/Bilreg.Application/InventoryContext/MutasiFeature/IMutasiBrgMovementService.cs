using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IMutasiBrgMovementService
{
    void DeductStock(MutasiBrgModel mutasi, SqlConnection conn, SqlTransaction trans);
    void RestoreStock(MutasiBrgModel mutasi, SqlConnection conn, SqlTransaction trans);
}
