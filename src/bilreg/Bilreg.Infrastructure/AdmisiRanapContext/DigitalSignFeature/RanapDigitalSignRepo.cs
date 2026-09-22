using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public class RanapDigitalSignRepo : IRanapDigitalSignRepo
{
    private readonly IRanapDigitalSignDal _dal;

    public RanapDigitalSignRepo(IRanapDigitalSignDal dal) => _dal = dal;

    public void SaveChanges(RanapDigitalSignModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(RanapDigitalSignDto.FromModel(model)),
                onNone: () => _dal.Insert(RanapDigitalSignDto.FromModel(model)));
    }

    public MayBe<RanapDigitalSignModel> LoadEntity(IRanapDigitalSignKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<RanapDigitalSignModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public MayBe<RanapDigitalSignModel> LoadByRegDokumen(string regId, string dokumenId)
    {
        var dto = _dal.GetByRegDokumen(regId, dokumenId);
        if (dto is null)
            return MayBe<RanapDigitalSignModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<RanapDigitalSignModel> ListByRegId(string regId)
    {
        var listDto = _dal.ListByRegId(regId)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }
}
