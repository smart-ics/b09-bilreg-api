using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLedgerScopeStateRepo : IStockLedgerScopeStateRepo
{
    private readonly IStockLedgerScopeDal _dal;

    public StockLedgerScopeStateRepo(IStockLedgerScopeDal dal) => _dal = dal;

    public void SaveChanges(StockLedgerScopeStateModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(StockLedgerScopeDto.FromModel(model)),
                onNone: () => _dal.Insert(StockLedgerScopeDto.FromModel(model)));
    }

    public MayBe<StockLedgerScopeStateModel> LoadEntity(IStockLedgerScopeKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<StockLedgerScopeStateModel>.None;
        return MayBe.From(dto.ToModel());
    }
}
