using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLegacyScopeRepo : IStockLegacyScopeRepo
{
    private readonly IStokLegacyScopeDal _dal;

    public StockLegacyScopeRepo(IStokLegacyScopeDal dal) => _dal = dal;

    public MayBe<StockLegacyScopeModel> LoadEntity(IStockLegacyScopeKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null
            ? MayBe<StockLegacyScopeModel>.None
            : MayBe.From(dto.ToModel());
    }

    public void SaveChanges(StockLegacyScopeModel model)
    {
        var at = DateTime.Now;
        var dto = StokLegacyScopeDto.FromModel(model, crtUser: "STL", crtDate: at, updUser: "STL", updDate: at);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }
}
