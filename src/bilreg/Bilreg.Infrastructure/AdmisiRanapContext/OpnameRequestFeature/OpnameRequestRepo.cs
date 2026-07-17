using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;

public class OpnameRequestRepo : IOpnameRequestRepo
{
    private readonly IOpnameRequestDal _dal;

    public OpnameRequestRepo(IOpnameRequestDal dal) => _dal = dal;

    public void SaveChanges(OpnameRequestModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(OpnameRequestDto.FromModel(model)),
                onNone: () => _dal.Insert(OpnameRequestDto.FromModel(model)));
    }

    public MayBe<OpnameRequestModel> LoadEntity(IOpnameRequestKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<OpnameRequestModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<OpnameRequestModel> ListData(OpnameRequestListFilter filter)
    {
        var listDto = _dal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }
}
