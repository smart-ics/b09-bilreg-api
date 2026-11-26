using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface ITerimaMutasiMovementService
{
    void SettleReceivedStock(TerimaMutasiModel terimaMutasi, SqlConnection conn, SqlTransaction trans);
}
