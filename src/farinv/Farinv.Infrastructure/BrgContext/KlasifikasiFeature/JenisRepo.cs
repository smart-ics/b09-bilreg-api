using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class JenisRepo : IJenisRepo
{
    private readonly IJenisDal _jenisDal;

    public JenisRepo(IJenisDal jenisDal)
    {
        _jenisDal = jenisDal;
    }

    public void SaveChanges(JenisType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _jenisDal.Update(JenisDto.FromModel(model)),
                onNone: () => _jenisDal.Insert(JenisDto.FromModel(model)));
    }

    public MayBe<JenisType> LoadEntity(IJenisKey key)
    {
        var dto = _jenisDal.GetData(key);
        if (dto is null)
            return MayBe<JenisType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IJenisKey key)
    {
        _jenisDal.Delete(key);
    }

    public IEnumerable<JenisType> ListData()
    {
        var listDto = _jenisDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}