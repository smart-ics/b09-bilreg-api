using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class FormulariumRepo : IFormulariumRepo
{
    private readonly IFormulariumDal _formulariumDal;

    public FormulariumRepo(IFormulariumDal formulariumDal)
    {
        _formulariumDal = formulariumDal;
    }

    public void SaveChanges(FormulariumType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _formulariumDal.Update(FormulariumDto.FromModel(model)),
                onNone: () => _formulariumDal.Insert(FormulariumDto.FromModel(model)));
    }

    public MayBe<FormulariumType> LoadEntity(IFormulariumKey key)
    {
        var dto = _formulariumDal.GetData(key);
        if (dto is null)
            return MayBe<FormulariumType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IFormulariumKey key)
    {
        _formulariumDal.Delete(key);
    }

    public IEnumerable<FormulariumType> ListData()
    {
        var listDto = _formulariumDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
