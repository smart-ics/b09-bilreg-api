using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class PabrikRepo : IPabrikRepo
{
    private readonly IPabrikDal _pabrikDal;

    public PabrikRepo(IPabrikDal pabrikDal)
    {
        _pabrikDal = pabrikDal;
    }

    public void SaveChanges(PabrikType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _pabrikDal.Update(PabrikDto.FromModel(model)),
                onNone: () => _pabrikDal.Insert(PabrikDto.FromModel(model)));
    }

    public MayBe<PabrikType> LoadEntity(IPabrikKey key)
    {
        var dto = _pabrikDal.GetData(key);
        if (dto is null)
            return MayBe<PabrikType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IPabrikKey key)
    {
        _pabrikDal.Delete(key);
    }

    public IEnumerable<PabrikType> ListData()
    {
        var listDto = _pabrikDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
