using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLegacyBindingRepo : IStockLegacyBindingRepo
{
    private readonly IStokLegacyBindingDal _dal;

    public StockLegacyBindingRepo(IStokLegacyBindingDal dal) => _dal = dal;

    public void Insert(StockLegacyBindingModel model)
    {
        var at = DateTime.Now;
        _dal.Insert(StokLegacyBindingDto.FromModel(model, crtUser: "STL", crtDate: at, updUser: "STL", updDate: at));
    }

    public MayBe<StockLegacyBindingModel> FindByLegacyBukuId(string legacyBukuId)
    {
        var dto = _dal.GetByLegacyBukuId(legacyBukuId);
        return dto is null
            ? MayBe<StockLegacyBindingModel>.None
            : MayBe.From(dto.ToModel());
    }

    public MayBe<StockLegacyBindingModel> FindByStokMutasiId(string stokMutasiId)
    {
        var dto = _dal.GetByStokMutasiId(stokMutasiId);
        return dto is null
            ? MayBe<StockLegacyBindingModel>.None
            : MayBe.From(dto.ToModel());
    }
}
