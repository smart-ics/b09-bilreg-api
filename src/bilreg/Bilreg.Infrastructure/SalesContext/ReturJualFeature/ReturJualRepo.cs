using Bilreg.Application.SalesContext.ReturJualFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public class ReturJualRepo : IReturJualRepo
{
    private readonly IReturJualDal _returJualDal;
    private readonly IReturJualItemDal _returJualItemDal;

    public ReturJualRepo(IReturJualDal returJualDal, 
        IReturJualItemDal returJualItemDal)
    {
        _returJualDal = returJualDal;
        _returJualItemDal = returJualItemDal;
    }

    public void SaveChanges(ReturJualModel model)
    {
        var dto = ReturJualDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _returJualDal.Update(dto),
                onNone: () => _returJualDal.Insert(dto));

        var listItemDto = ReturJualItemDto.FlattenFromModel(model);
        _returJualItemDal.Delete(model);
        _returJualItemDal.Insert(listItemDto);
    }

    public void DeleteEntity(IReturJualKey key)
    {
        _returJualItemDal.Delete(key);
        _returJualDal.Delete(key);
    }
    public MayBe<ReturJualModel> LoadEntity(IReturJualKey key)
    {
        var dto = _returJualDal.GetData(key);
        if (dto is null)
            return MayBe<ReturJualModel>.None;

        var listItem = _returJualItemDal.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(listItem);
        return MayBe.From(model);
    }

    public IEnumerable<ReturJualModel> ListData(IRegKey filter)
    {
        var listDto = _returJualDal.ListData(filter)?.ToList() ?? [];
        var result = new List<ReturJualModel>();

        foreach (var dto in listDto)
        {
            var listItem = _returJualItemDal.ListData(ReturJualModel.Key(dto.ReturJualId))?.ToList() ?? [];
            result.Add(dto.ToModel(listItem));
        }

        return result;
    }
}
