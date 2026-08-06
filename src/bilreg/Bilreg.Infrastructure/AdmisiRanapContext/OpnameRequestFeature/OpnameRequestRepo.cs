using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;

public class OpnameRequestRepo : IOpnameRequestRepo
{
    private readonly IOpnameRequestDal _dal;
    private readonly IOpnameRequestInsuranceDal _insuranceDal;
    public OpnameRequestRepo(IOpnameRequestDal dal, 
        IOpnameRequestInsuranceDal insuranceDal)
    {
        _dal = dal;
        _insuranceDal = insuranceDal;
    }

    public void SaveChanges(OpnameRequestModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(OpnameRequestDto.FromModel(model)),
                onNone: () => _dal.Insert(OpnameRequestDto.FromModel(model)));

        var insurance = _insuranceDal.GetData(model);
        var insuranceDto = OpnameRequestInsuranceDto.FromModel(model, model.Insurance);
        if (insurance is null)
            _insuranceDal.Insert(insuranceDto);
        else
            _insuranceDal.Update(insuranceDto);

    }

    public MayBe<OpnameRequestModel> LoadEntity(IOpnameRequestKey key)
    {
        var dto = _dal.GetData(key);
        var insDto = _insuranceDal.GetData(key)
            ?? new OpnameRequestInsuranceDto(key.OpnameRequestId, "-", "-", "-");

        if (dto is null)
            return MayBe<OpnameRequestModel>.None;
        return MayBe.From(dto.ToModel(insDto));
    }

    public IEnumerable<OpnameRequestModel> ListData(OpnameRequestListFilter filter)
    {
        var listDto = _dal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }

    public MayBe<OpnameRequestModel> GetByEmrOrder(string emrOrderId)
    {
        var dto = _dal.GetByEmrOrder(emrOrderId);
        if (dto is null)
            return MayBe<OpnameRequestModel>.None;
        var key = OpnameRequestModel.Key(dto.OpnameRequestId);
        var insDto = _insuranceDal.GetData(key)
            ?? new OpnameRequestInsuranceDto(key.OpnameRequestId, "-", "-", "-");

        
        return MayBe.From(dto.ToModel(insDto));
    }
}
