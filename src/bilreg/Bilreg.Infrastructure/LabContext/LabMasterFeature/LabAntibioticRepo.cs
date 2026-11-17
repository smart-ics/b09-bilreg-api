using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabAntibioticRepo : ILabAntibioticRepo
{
    private readonly ILabAntibioticDal _dal;

    public LabAntibioticRepo(ILabAntibioticDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(LabAntibioticModel model)
    {
        var dto = LabAntibioticDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<LabAntibioticModel> LoadEntity(ILabAntibioticKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<LabAntibioticModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<LabAntibioticModel> ListData()
    {
        return (_dal.ListData() ?? Enumerable.Empty<LabAntibioticDto>()).Select(x => x.ToModel());
    }

    public IEnumerable<LabAntibioticModel> ListData(string? search)
    {
        return (_dal.ListData(search) ?? Enumerable.Empty<LabAntibioticDto>()).Select(x => x.ToModel());
    }
}
