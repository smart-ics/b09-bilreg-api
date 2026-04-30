using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.CoaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.CoaFeature;

public class CoaRepo : ICoaRepo
{
    private readonly ICoaDal _coaDal;
    public CoaRepo(ICoaDal coaDal)
    {
        _coaDal = coaDal;
    }
    public MayBe<CoaType> LoadEntity(ICoaKey key)
    {
        var dto = _coaDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }
    public IEnumerable<CoaType> ListData()
    {
        var listDto = _coaDal.ListData();
        return listDto.Select(dto => dto.ToModel());
    }
}
