using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionRepo : ILabTestDefinitionRepo
{
    private readonly ILabTestDefinitionDal _definitionDal;
    private readonly ILabTestComponentDal _componentDal;

    public LabTestDefinitionRepo(ILabTestDefinitionDal definitionDal, ILabTestComponentDal componentDal)
    {
        _definitionDal = definitionDal;
        _componentDal = componentDal;
    }

    public void SaveChanges(LabTestDefinitionModel model)
    {
        var dto = LabTestDefinitionDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _definitionDal.Update(dto),
                onNone: () => _definitionDal.Insert(dto));

        var componentDtos = model.Components
            .Select(x => LabTestComponentDto.FromModel(model.TestDefinitionId, x))
            .ToList();

        _componentDal.Delete(model);
        _componentDal.Insert(componentDtos);
    }

    public MayBe<LabTestDefinitionModel> LoadEntity(ILabTestDefinitionKey key)
    {
        var dto = _definitionDal.GetData(key);
        if (dto is null)
            return MayBe<LabTestDefinitionModel>.None;

        var components = _componentDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        return MayBe.From(dto.ToModel(components));
    }

    public IEnumerable<LabTestDefinitionModel> ListData(LabTestDefinitionListFilter filter)
    {
        return _definitionDal.ListData(filter)
            .Select(dto =>
            {
                var components = _componentDal.ListData(LabTestDefinitionModel.Key(dto.TestDefinitionId))
                    ?.Select(x => x.ToModel())
                    .ToList() ?? [];
                return dto.ToModel(components);
            });
    }

    public MayBe<LabTestDefinitionModel> LoadActiveByTarifId(string tarifId)
    {
        var dto = _definitionDal.GetActiveByTarifId(tarifId, null);
        if (dto is null)
            return MayBe<LabTestDefinitionModel>.None;

        var testDefinitionKey = LabTestDefinitionModel.Key(dto.TestDefinitionId);
        var listComponentDto = _componentDal.ListData(testDefinitionKey)?.ToList() ?? [];
        var components = listComponentDto
            .Select(x => x.ToModel())
            .ToList() ?? [];
        return MayBe.From(dto.ToModel(components));
    }

    public bool HasActiveTarifConflict(string tarifId, string? excludeTestDefinitionId) =>
        _definitionDal.GetActiveByTarifId(tarifId, excludeTestDefinitionId) is not null;

    public string AllocateNextTestDefinitionId()
    {
        var maxId = _definitionDal.GetMaxTestDefinitionId();
        var next = 1;
        if (!string.IsNullOrWhiteSpace(maxId) && LabMasterIdFormat.IsValidLtd(maxId))
            next = LabMasterIdFormat.ParseLtdSuffix(maxId) + 1;

        if (next > 0xFFFF)
            throw new InvalidOperationException("LAB_LTD_EXHAUSTED: LabTestDefinition ID sequence exhausted.");

        return LabMasterIdFormat.FormatLtd(next);
    }
}
