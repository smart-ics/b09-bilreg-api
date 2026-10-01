using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.Shared;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class TindakanLanjutRepo : ITindakanLanjutRepo
{
    private readonly ITindakanLanjutDal _tindakanLanjutDal;
    private readonly ITindakanLanjutItemDal _itemDal;

    public TindakanLanjutRepo(
        ITindakanLanjutDal tindakanLanjutDal,
        ITindakanLanjutItemDal itemDal)
    {
        _tindakanLanjutDal = tindakanLanjutDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(TindakanLanjutModel model)
    {
        var exists = _tindakanLanjutDal.GetData(model) is not null;
        
        var dto = TindakanLanjutDto.FromModel(model);
        var itemDtos = model.ListItem
            .Select(x => TindakanLanjutItemDto.FromModel(x, model.TindakanLanjutId))
            .ToList();

        if (exists)
        {
            //  Optimistic state write incl. RowVersion: a late cancellation
            //  cannot overwrite a confirmed reception (rows == 0 -> conflict).
            var rows = _tindakanLanjutDal.UpdateState(dto, model.RowVersion - 1);
            if (rows == 0)
            {
                var current = _tindakanLanjutDal.GetData(model);
                throw new TindakanLanjutConcurrencyException(
                    model.TindakanLanjutId, model.RowVersion - 1,
                    current?.RowVersion ?? -1);
            }
            _itemDal.Delete(model);
            if (itemDtos.Count > 0)
                _itemDal.Insert(itemDtos);
        }
        else
        {
            //  New orders: single full-row insert in its own transaction scope
            //  (TD-08 — never joins the clinical-action transaction).
            _tindakanLanjutDal.Insert(dto);
            if (itemDtos.Count > 0)
                _itemDal.Insert(itemDtos);
        }
    }

    public MayBe<TindakanLanjutModel> LoadEntity(ITindakanLanjutKey key)
    {
        var dto = _tindakanLanjutDal.GetData(key);
        if (dto is null)
            return MayBe<TindakanLanjutModel>.None;

        var itemDtos = _itemDal.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(itemDtos.Select(x => x.ToModel()));
        return MayBe.From(model);
    }

    public IEnumerable<TindakanLanjutModel> ListData(IRegKey regKey)
    {
        var listDto = _tindakanLanjutDal.ListData(regKey)?.ToList() ?? [];
        var result = listDto.Select(x =>
        {
            var itemDtos = _itemDal.ListData(TindakanLanjutModel.Key(x.TindakanLanjutId))?.ToList() ?? [];
            return x.ToModel(itemDtos.Select(y => y.ToModel()));
        });
        return result;
    }

    public IEnumerable<TindakanLanjutModel> ListOutstanding(IRegKey regKey)
    {
        var listDto = _tindakanLanjutDal.ListOutstanding(regKey)?.ToList() ?? [];
        var result = listDto.Select(x =>
        {
            var itemDtos = _itemDal.ListData(TindakanLanjutModel.Key(x.TindakanLanjutId))?.ToList() ?? [];
            return x.ToModel(itemDtos.Select(y => y.ToModel()));
        });
        return result;
    }

    public IEnumerable<TindakanLanjutModel> ListOutstandingAll()
    {
        var listDto = _tindakanLanjutDal.ListOutstandingAll()?.ToList() ?? [];
        var result = listDto.Select(x =>
        {
            var itemDtos = _itemDal.ListData(TindakanLanjutModel.Key(x.TindakanLanjutId))?.ToList() ?? [];
            return x.ToModel(itemDtos.Select(y => y.ToModel()));
        });
        return result;
    }
}