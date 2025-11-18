using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class OpCaseRepo : IOpCaseRepo
{
    private readonly IOpCaseDal _opCaseDal;
    private readonly IOpCaseAktifDal _opCaseAktifDal;

    public OpCaseRepo(IOpCaseDal opCaseDal, 
        IOpCaseAktifDal opCaseAktifDal)
    {
        _opCaseDal = opCaseDal;
        _opCaseAktifDal = opCaseAktifDal;
    }

    public void SaveChanges(OpCaseModel model)
    {
        var opCaseDto = OpCaseDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _opCaseDal.Update(opCaseDto),
                onNone: () => _opCaseDal.Insert(opCaseDto)
            );
        
        if (model.ActiveOpCase is null)
            _opCaseAktifDal.Delete(model);
        else
        {
            var opCaseAktifDto = OpCaseAktifDto.FromModel(model.ActiveOpCase);
            var opCaseAktifDb = _opCaseAktifDal.GetData(model);
            if (opCaseAktifDb is null)
                _opCaseAktifDal.Insert(opCaseAktifDto);
            else
                _opCaseAktifDal.Update(opCaseAktifDto);
        }
    }

    public MayBe<OpCaseModel> LoadEntity(IOrderOpKey key)
    {
        var opCaseDto = _opCaseDal.GetData(key);
        if (opCaseDto is null)
            return MayBe<OpCaseModel>.None;
        
        var result = opCaseDto.ToModel();
        return MayBe.From(result);
    }

    public void DeleteEntity(IOrderOpKey key)
    {
        _opCaseDal.Delete(key);
        _opCaseAktifDal.Delete(key);
    }

    public IEnumerable<OpCaseModel> ListData(Periode filter)
    {
        var listDto = _opCaseDal.ListData(filter);
        return listDto.Select(x => x.ToModel());
    }

    public IEnumerable<OpCaseReff> ListActiveOpCase()
    {
        var listDto = _opCaseAktifDal.ListData();
        return listDto.Select(x => x.ToModel()).ToList();
    }
}