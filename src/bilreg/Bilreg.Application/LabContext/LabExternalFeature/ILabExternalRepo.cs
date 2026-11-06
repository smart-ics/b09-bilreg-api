using Bilreg.Domain.LabContext.LabExternalFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public interface ILabExternalRepo :
    ISaveChange<LabExternalModel>,
    ILoadEntity<LabExternalModel, ILabExternalKey>
{
    MayBe<LabExternalModel> LoadByExternalMrNo(string externalMrNo);
    IEnumerable<LabExternalModel> ListData(string? searchTerm, DateTime? date1, DateTime? date2, int? status);
}
