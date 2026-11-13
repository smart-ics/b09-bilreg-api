using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabDisciplineRepo : ILabDisciplineRepo
{
    private readonly ILabDisciplineDal _dal;

    public LabDisciplineRepo(ILabDisciplineDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(LabDisciplineModel model)
    {
        var dto = LabDisciplineDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<LabDisciplineModel> LoadEntity(ILabDisciplineKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<LabDisciplineModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<LabDisciplineModel> ListData()
    {
        return (_dal.ListData() ?? Enumerable.Empty<LabDisciplineDto>()).Select(x => x.ToModel());
    }

    public IEnumerable<LabDisciplineModel> ListData(string? search)
    {
        return (_dal.ListData(search) ?? Enumerable.Empty<LabDisciplineDto>()).Select(x => x.ToModel());
    }
}
