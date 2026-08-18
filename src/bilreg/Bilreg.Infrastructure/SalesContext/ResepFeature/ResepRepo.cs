using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public class ResepRepo : IResepRepo
{
    private readonly IResepDal _resepDal;
    private readonly IResepBrgDal _resepBrgDal;

    public ResepRepo(IResepDal resepDal, IResepBrgDal resepBrgDal)
    {
        _resepDal = resepDal;
        _resepBrgDal = resepBrgDal;
    }

    public void SaveChanges(ResepModel model)
    {
        var dto = ResepDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _resepDal.Update(dto),
                onNone: () => _resepDal.Insert(dto));

        var listBrgDto = ResepBrgDto.FlattenFromModel(model);
        _resepBrgDal.Delete(model);
        _resepBrgDal.Insert(listBrgDto);
    }

    public MayBe<ResepModel> LoadEntity(IResepKey key)
    {
        var dto = _resepDal.GetData(key);
        if (dto is null)
            return MayBe<ResepModel>.None;

        var listBrg = _resepBrgDal.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(listBrg);
        return MayBe.From(model);
    }

    public void DeleteEntity(IResepKey key)
    {
        _resepBrgDal.Delete(key);
        _resepDal.Delete(key);
    }

    public IEnumerable<ResepModel> ListData(IRegKey filter)
    {
        var listDto = _resepDal.ListData(filter)?.ToList() ?? [];
        var result = new List<ResepModel>();

        foreach (var dto in listDto)
        {
            var listBrg = _resepBrgDal.ListData(ResepModel.Key(dto.ResepId))?.ToList() ?? [];
            result.Add(dto.ToModel(listBrg));
        }

        return result;
    }
}
