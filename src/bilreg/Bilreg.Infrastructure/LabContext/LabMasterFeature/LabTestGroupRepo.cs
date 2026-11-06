using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabTestGroupRepo : ILabTestGroupRepo
{
    private readonly ILabTestGroupDal _dal;

    public LabTestGroupRepo(ILabTestGroupDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(LabTestGroupModel model)
    {
        var dto = LabTestGroupDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<LabTestGroupModel> LoadEntity(ILabTestGroupKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<LabTestGroupModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<LabTestGroupModel> ListData()
    {
        return (_dal.ListData() ?? Enumerable.Empty<LabTestGroupDto>()).Select(x => x.ToModel());
    }

    public IEnumerable<LabTestGroupModel> ListData(string? search)
    {
        return (_dal.ListData(search) ?? Enumerable.Empty<LabTestGroupDto>()).Select(x => x.ToModel());
    }
}
