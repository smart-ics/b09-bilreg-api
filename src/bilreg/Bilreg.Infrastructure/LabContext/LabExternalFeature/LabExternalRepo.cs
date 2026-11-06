using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabExternalFeature;

public class LabExternalRepo : ILabExternalRepo
{
    private readonly ILabExternalDal _dal;

    public LabExternalRepo(ILabExternalDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(LabExternalModel model)
    {
        var dto = LabExternalDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<LabExternalModel> LoadEntity(ILabExternalKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<LabExternalModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public MayBe<LabExternalModel> LoadByExternalMrNo(string externalMrNo)
    {
        var dto = _dal.GetByExternalMrNo(externalMrNo);
        if (dto is null)
            return MayBe<LabExternalModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<LabExternalModel> ListData(string? searchTerm, DateTime? date1, DateTime? date2, int? status)
    {
        var dtos = _dal.ListData(searchTerm, date1, date2, status);
        return dtos?.Select(d => d.ToModel()) ?? [];
    }
}
