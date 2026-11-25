using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Application.InventoryContext.OpnameFeature.Ports;

public interface IStockOpnameAdjustmentRepo
{
    void Insert(StockOpnameAdjustmentModel model);
    void Insert(IEnumerable<StockOpnameAdjustmentModel> models);
    IEnumerable<StockOpnameAdjustmentModel> ListBySessionId(string sessionId);
    bool ExistsForSession(string sessionId);
    bool ExistsForTuple(string sessionId, string brgId, string batchNo, string expiredDate);
}
