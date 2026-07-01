using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public interface ILabTestDefinitionRepo :
    ISaveChange<LabTestDefinitionModel>,
    ILoadEntity<LabTestDefinitionModel, ILabTestDefinitionKey>,
    IListData<LabTestDefinitionModel, LabTestDefinitionListFilter>
{
    MayBe<LabTestDefinitionModel> LoadActiveByTarifId(string tarifId);
    bool HasActiveTarifConflict(string tarifId, string? excludeTestDefinitionId);
    string AllocateNextTestDefinitionId();
}
