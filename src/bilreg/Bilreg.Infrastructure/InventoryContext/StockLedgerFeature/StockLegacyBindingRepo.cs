using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLegacyBindingRepo : IStockLegacyBindingRepo
{
    private readonly IStokLegacyBindingDal _dal;

    public StockLegacyBindingRepo(IStokLegacyBindingDal dal) => _dal = dal;

    public void Insert(StockLegacyBindingModel model) => Insert(model, userId: "STL");

    public void Insert(StockLegacyBindingModel model, string userId)
    {
        var at = DateTime.Now;
        var auditUser = string.IsNullOrWhiteSpace(userId) ? "STL" : userId;
        _dal.Insert(StokLegacyBindingDto.FromModel(model, crtUser: auditUser, crtDate: at, updUser: auditUser, updDate: at));
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
