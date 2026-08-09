using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockLayerLegacyBindingRepo : IStockLayerLegacyBindingRepo
{
    private readonly IStockLayerLegacyBindingDal _dal;

    public StockLayerLegacyBindingRepo(IStockLayerLegacyBindingDal dal)
    {
        _dal = dal ?? throw new ArgumentNullException(nameof(dal));
    }

    public void SaveChanges(StockLayerLegacyBindingType binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        var dto = StockLayerLegacyBindingDto.FromModel(binding);
        var existing = _dal.GetData(binding.StockLayerId);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<StockLayerLegacyBindingType> LoadByStockLayerId(string stockLayerId)
    {
        if (string.IsNullOrWhiteSpace(stockLayerId))
            return MayBe<StockLayerLegacyBindingType>.None;

        var dto = _dal.GetData(stockLayerId.Trim());
        return dto is null
            ? MayBe<StockLayerLegacyBindingType>.None
            : MayBe.From(dto.ToModel());
    }

    public IReadOnlyList<StockLayerLegacyBindingType> ListByLedgerScope(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return _dal.ListByLedgerScope(scope).Select(x => x.ToModel()).ToList();
    }

    public IReadOnlyList<StockLayerLegacyBindingType> ListByWriteScope(IStockWriteScopeKey writeScope)
    {
        ArgumentNullException.ThrowIfNull(writeScope);
        return _dal.ListByWriteScope(writeScope).Select(x => x.ToModel()).ToList();
    }
}
