using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class FornasRepo : IFornasRepo
{
    private readonly IFornasDal _fornasDal;

    public FornasRepo(IFornasDal fornasDal)
    {
        _fornasDal = fornasDal;
    }

    public MayBe<FornasType> LoadEntity(IFornasKey key)
    {
        var dto = _fornasDal.GetData(key);
        if (dto is null)
            return MayBe<FornasType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public IEnumerable<FornasType> ListData()
    {
        var listDto = _fornasDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
