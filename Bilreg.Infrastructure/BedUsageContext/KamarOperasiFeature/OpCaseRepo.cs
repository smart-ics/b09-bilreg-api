using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class OpCaseRepo : IOpCaseRepo
{
    private readonly IOpCaseDal _opCaseDal;
    private readonly IOpCaseStateHistDal _opCaseStateHistDal;
    private readonly IOpCaseAktifDal _opCaseAktifDal;
    private readonly IOpCasePpaDal _opCasePpaDal;

    public OpCaseRepo(IOpCaseDal opCaseDal, 
        IOpCaseStateHistDal opCaseStateHistDal, 
        IOpCaseAktifDal opCaseAktifDal, 
        IOpCasePpaDal opCasePpaDal)
    {
        _opCaseDal = opCaseDal;
        _opCaseStateHistDal = opCaseStateHistDal;
        _opCaseAktifDal = opCaseAktifDal;
        _opCasePpaDal = opCasePpaDal;
    }

    public void SaveChanges(OpCaseModel model)
    {
        var opCaseDto = OpCaseDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _opCaseDal.Update(opCaseDto),
                onNone: () => _opCaseDal.Insert(opCaseDto)
            );
        
        var listStateHist = model.ListStateHistory.Select(x => OpCaseStateHistDto.FromModel(model.OrderOpId, x)).ToList();
        _opCaseStateHistDal.Delete(model);
        _opCaseStateHistDal.Insert(listStateHist);
        
        var listPpa = model.ListPpa.Select(x => OpCasePpaDto.FromModel(model.OrderOpId, x)).ToList();
        _opCasePpaDal.Delete(model);
        _opCasePpaDal.Insert(listPpa);
        
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
        
        var listStateHistDto = _opCaseStateHistDal.ListData(key)?.ToList() ?? [];
        var listStateHistType = listStateHistDto.Select(x => x.ToModel()).ToList();
        var listPpaDto = _opCasePpaDal.ListData(key)?.ToList() ?? [];
        var listPpaType = listPpaDto.Select(x => x.ToModel()).ToList();
        
        var result = opCaseDto.ToModel(listStateHistType, listPpaType);
        return MayBe.From(result);
    }

    public void DeleteEntity(IOrderOpKey key)
    {
        _opCaseDal.Delete(key);
        _opCaseAktifDal.Delete(key);
        _opCaseStateHistDal.Delete(key);
    }

    public IEnumerable<OpCaseOrderView> ListActiveOpCase()
    {
        var listDto = _opCaseAktifDal.ListData()?.ToList() ?? [];
        return listDto.Select(x => x.ToView()).ToList();
    }
}