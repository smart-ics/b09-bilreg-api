using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class JenisTarifRepo : IJenisTarifRepo
{
    private readonly IJenisTarifDal _jenisTarifDal;
    public JenisTarifRepo(IJenisTarifDal jenisTarifDal)
    {
        _jenisTarifDal = jenisTarifDal;
    }
    public void SaveChanges(JenisTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _jenisTarifDal.Update(JenisTarifDto.FromModel(model)),
                onNone: () => _jenisTarifDal.Insert(JenisTarifDto.FromModel(model)));
    }

    public MayBe<JenisTarifType> LoadEntity(IJenisTarifKey key)
    {   
        var dto = _jenisTarifDal.GetData(key);
        if (dto is null)
            return MayBe<JenisTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IJenisTarifKey key)
    {
        _jenisTarifDal.Delete(key);
    }

    public IEnumerable<JenisTarifType> ListData()
    {
        var listDto = _jenisTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
