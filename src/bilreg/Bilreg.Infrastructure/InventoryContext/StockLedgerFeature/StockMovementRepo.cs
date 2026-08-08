using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockMovementRepo : IStockMovementRepo
{
    private readonly IStockMovementDal _headerDal;
    private readonly IStockMovementLineDal _lineDal;

    public StockMovementRepo(IStockMovementDal headerDal, IStockMovementLineDal lineDal)
    {
        _headerDal = headerDal;
        _lineDal = lineDal;
    }

    public void SaveChanges(StockMovementModel model)
    {
        if (_headerDal.GetData(model) is not null)
        {
            throw StockLedgerPersistenceException.Immutable(
                $"Stock Movement '{model.StockMovementId}' is immutable and already persisted.");
        }

        _headerDal.Insert(StockMovementDto.FromModel(model));
        _lineDal.Insert(model.Lines.Select(x => StockMovementLineDto.FromModel(model.StockMovementId, x)));
    }

    public MayBe<StockMovementModel> LoadEntity(IStockMovementKey key)
    {
        var header = _headerDal.GetData(key);
        if (header is null)
            return MayBe<StockMovementModel>.None;

        var lines = (_lineDal.ListData(key) ?? [])
            .Select(x => x.ToModel())
            .ToArray();

        if (lines.Length == 0)
        {
            throw StockLedgerPersistenceException.Integrity(
                $"Stock Movement '{key.StockMovementId}' has no durable lines.");
        }

        return MayBe.From(header.ToModel(lines));
    }
}
