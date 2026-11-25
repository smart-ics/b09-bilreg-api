using Bilreg.Domain.InventoryContext.OpnameFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.OpnameFeature.Ports;

public interface IStockOpnameSessionRepo :
    ISaveChange<StockOpnameSessionModel>,
    ILoadEntity<StockOpnameSessionModel, IStockOpnameSessionKey>
{
    bool HasActiveSessionAtLocation(string layananId);
    IEnumerable<StockOpnameSessionModel> ListData(string? layananId = null, SessionStatusEnum? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    bool HasTupleBeenCountedByUser(string sessionId, string brgId, string batchNo, string expiredDate, string userId);
}
