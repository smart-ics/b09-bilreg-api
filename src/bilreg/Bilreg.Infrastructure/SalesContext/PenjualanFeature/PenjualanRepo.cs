using Bilreg.Application.SalesContext.PenjualanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public class PenjualanRepo : IPenjualanRepo
{
    private readonly IPenjualanDal _penjualanDal;
    private readonly IPenjualanItemDal _penjualanItemDal;
    private readonly IPenjualanItemBatchDal _penjualanItemBatchDal;

    public PenjualanRepo(
        IPenjualanDal penjualanDal,
        IPenjualanItemDal penjualanItemDal,
        IPenjualanItemBatchDal penjualanItemBatchDal)
    {
        _penjualanDal = penjualanDal;
        _penjualanItemDal = penjualanItemDal;
        _penjualanItemBatchDal = penjualanItemBatchDal;
    }

    public PenjualanRepo(
        IPenjualanDal penjualanDal,
        IPenjualanItemDal penjualanItemDal)
        : this(penjualanDal, penjualanItemDal, null!)
    {
    }

    public void SaveChanges(PenjualanModel model)
    {
        var dto = PenjualanDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _penjualanDal.Update(dto),
                onNone: () => _penjualanDal.Insert(dto));

        var listItemDto = PenjualanItemDto.FlattenFromModel(model);
        _penjualanItemDal.Delete(model);
        _penjualanItemDal.Insert(listItemDto);

        if (_penjualanItemBatchDal is not null)
        {
            var listBatchDto = PenjualanItemBatchDto.FlattenFromModel(model);
            _penjualanItemBatchDal.Delete(model);
            _penjualanItemBatchDal.Insert(listBatchDto);
        }
    }

    public MayBe<PenjualanModel> LoadEntity(IPenjualanKey key)
    {
        var dto = _penjualanDal.GetData(key);
        if (dto is null)
            return MayBe<PenjualanModel>.None;

        var listItem = _penjualanItemDal.ListData(key)?.ToList() ?? [];
        var listBatch = _penjualanItemBatchDal?.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(listItem, listBatch);
        return MayBe.From(model);
    }

    public void DeleteEntity(IPenjualanKey key)
    {
        _penjualanItemBatchDal?.Delete(key);
        _penjualanItemDal.Delete(key);
        _penjualanDal.Delete(key);
    }

    public IEnumerable<PenjualanModel> ListData(IRegKey filter)
    {
        var listDto = _penjualanDal.ListData(filter)?.ToList() ?? [];
        var result = new List<PenjualanModel>();

        foreach (var dto in listDto)
        {
            var key = PenjualanModel.Key(dto.PenjualanId);
            var listItem = _penjualanItemDal.ListData(key)?.ToList() ?? [];
            var listBatch = _penjualanItemBatchDal?.ListData(key)?.ToList() ?? [];
            result.Add(dto.ToModel(listItem, listBatch));
        }

        return result;
    }
}
