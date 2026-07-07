using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public class AdmissionRepo : IAdmissionRepo
{
    private readonly IAdmissionDal _dal;

    public AdmissionRepo(IAdmissionDal dal) => _dal = dal;

    public void SaveChanges(AdmissionModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(AdmissionDto.FromModel(model)),
                onNone: () => _dal.Insert(AdmissionDto.FromModel(model)));
    }

    public MayBe<AdmissionModel> LoadEntity(IRegKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<AdmissionModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<AdmissionModel> ListData(AdmissionListFilter filter)
    {
        var listDto = _dal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }
}
