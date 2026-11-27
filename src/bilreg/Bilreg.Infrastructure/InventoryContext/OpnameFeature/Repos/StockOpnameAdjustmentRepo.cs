using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Repos;

public class StockOpnameAdjustmentRepo : IStockOpnameAdjustmentRepo
{
    private readonly IStockOpnameAdjustmentDal _dal;

    public StockOpnameAdjustmentRepo(IStockOpnameAdjustmentDal dal)
    {
        _dal = dal;
    }

    public void Insert(StockOpnameAdjustmentModel model)
    {
        _dal.Insert(StockOpnameAdjustmentDto.FromModel(model));
    }

    public void Insert(IEnumerable<StockOpnameAdjustmentModel> models)
    {
        _dal.Insert(models.Select(StockOpnameAdjustmentDto.FromModel));
    }

    public IEnumerable<StockOpnameAdjustmentModel> ListBySessionId(string sessionId)
    {
        var dtos = _dal.ListBySession(sessionId) ?? [];
        return dtos.Select(d => d.ToModel());
    }

    public bool ExistsForSession(string sessionId) =>
        _dal.ExistsForSession(sessionId);

    public bool ExistsForTuple(string sessionId, string brgId, string batchNo, string expiredDate) =>
        _dal.ExistsForTuple(sessionId, brgId, batchNo, expiredDate);
}
