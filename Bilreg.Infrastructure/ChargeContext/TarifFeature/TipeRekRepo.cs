using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TipeRekRepo : ITipeRekRepo
{
    private readonly ITipeRekDal _tipeRekDal;
    public TipeRekRepo(ITipeRekDal tipeRekDal)
    {
        _tipeRekDal = tipeRekDal;
    }
    public void SaveChanges(TipeRekType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tipeRekDal.Update(TipeRekDto.FromModel(model)),
                onNone: () => _tipeRekDal.Insert(TipeRekDto.FromModel(model)));
    }

    public MayBe<TipeRekType> LoadEntity(ITipeRekKey key)
    {   
        var dto = _tipeRekDal.GetData(key);
        if (dto is null)
            return MayBe<TipeRekType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ITipeRekKey key)
    {
        _tipeRekDal.Delete(key);
    }

    public IEnumerable<TipeRekType> ListData()
    {
        var listDto = _tipeRekDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
