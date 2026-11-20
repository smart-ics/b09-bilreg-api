using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Repos;

public class StockOpnameSessionRepo : IStockOpnameSessionRepo
{
    private readonly IStockOpnameSessionDal _sessionDal;
    private readonly IStockOpnameScopeItemDal _scopeItemDal;
    private readonly IStockOpnameCountedItemDal _countedItemDal;
    private readonly IStockOpnameVarianceItemDal _varianceItemDal;
    private readonly IStockOpnameRecountResultDal _recountResultDal;

    public StockOpnameSessionRepo(
        IStockOpnameSessionDal sessionDal,
        IStockOpnameScopeItemDal scopeItemDal,
        IStockOpnameCountedItemDal countedItemDal,
        IStockOpnameVarianceItemDal varianceItemDal,
        IStockOpnameRecountResultDal recountResultDal)
    {
        _sessionDal = sessionDal;
        _scopeItemDal = scopeItemDal;
        _countedItemDal = countedItemDal;
        _varianceItemDal = varianceItemDal;
        _recountResultDal = recountResultDal;
    }

    public void SaveChanges(StockOpnameSessionModel model)
    {
        var existing = _sessionDal.GetData(model.SessionId);
        var sessionDto = StockOpnameSessionDto.FromModel(model);

        if (existing == null)
        {
            _sessionDal.Insert(sessionDto);
        }
        else
        {
            _sessionDal.Update(sessionDto);
        }

        // Persist ScopeItems
        if (model.ScopeItems.Count > 0)
        {
            _scopeItemDal.Delete(model.SessionId);
            _scopeItemDal.Insert(model.ScopeItems.Select(StockOpnameScopeItemDto.FromModel));
        }

        // Persist CountedItems
        if (model.CountedItems.Count > 0)
        {
            _countedItemDal.Delete(model.SessionId);
            _countedItemDal.Insert(model.CountedItems.Select(StockOpnameCountedItemDto.FromModel));
        }

        // Persist VarianceItems & RecountResults
        if (model.VarianceItems.Count > 0)
        {
            var existingVariances = (_varianceItemDal.ListBySession(model.SessionId) ?? [])
                .ToDictionary(x => x.VarianceItemId);

            foreach (var v in model.VarianceItems)
            {
                var vDto = StockOpnameVarianceItemDto.FromModel(v);
                if (existingVariances.ContainsKey(v.VarianceItemId))
                {
                    _varianceItemDal.Update(vDto);
                }
                else
                {
                    _varianceItemDal.Insert(vDto);
                }

                if (v.RecountResult != null)
                {
                    var rDto = StockOpnameRecountResultDto.FromModel(v.RecountResult);
                    var existingRecount = _recountResultDal.GetByVarianceItem(v.VarianceItemId);
                    if (existingRecount == null)
                    {
                        _recountResultDal.Insert(rDto);
                    }
                    else
                    {
                        _recountResultDal.Update(rDto);
                    }
                }
            }
        }
    }

    public MayBe<StockOpnameSessionModel> LoadEntity(IStockOpnameSessionKey key)
    {
        var sessionDto = _sessionDal.GetData(key.SessionId);
        if (sessionDto == null)
            return MayBe<StockOpnameSessionModel>.None;

        var scopeItemDtos = _scopeItemDal.ListBySession(key.SessionId) ?? [];
        var scopeItems = scopeItemDtos.Select(s => s.ToModel()).ToList();

        var countedItemDtos = _countedItemDal.ListBySession(key.SessionId) ?? [];
        var countedItems = countedItemDtos.Select(c => c.ToModel()).ToList();

        var varianceItemDtos = (_varianceItemDal.ListBySession(key.SessionId) ?? []).ToList();
        List<StockOpnameVarianceItemModel> varianceItems = [];

        if (varianceItemDtos.Count > 0)
        {
            var recountResultDtos = _recountResultDal.ListByVarianceItems(
                varianceItemDtos.Select(v => v.VarianceItemId)) ?? [];
            var recountMap = recountResultDtos.ToDictionary(r => r.VarianceItemId, r => r.ToModel());

            foreach (var vDto in varianceItemDtos)
            {
                recountMap.TryGetValue(vDto.VarianceItemId, out var recountModel);
                varianceItems.Add(vDto.ToModel(recountModel));
            }
        }

        var model = sessionDto.ToModel(scopeItems, countedItems, varianceItems);
        return MayBe.From(model);
    }

    public bool HasActiveSessionAtLocation(string layananId) =>
        _sessionDal.HasActiveSessionAtLocation(layananId);

    public IEnumerable<StockOpnameSessionModel> ListData(
        string? layananId = null,
        SessionStatusEnum? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var dtos = _sessionDal.ListData(
            layananId,
            status.HasValue ? (int)status.Value : null,
            fromDate,
            toDate) ?? [];

        return dtos.Select(d => d.ToModel());
    }

    public bool HasTupleBeenCountedByUser(
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string userId) =>
        _countedItemDal.HasTupleBeenCountedByUser(sessionId, brgId, batchNo, expiredDate, userId);
}
