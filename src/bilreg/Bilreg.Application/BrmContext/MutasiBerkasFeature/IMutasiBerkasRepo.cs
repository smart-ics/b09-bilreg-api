using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature;

public interface IMutasiBerkasRepo :
    ISaveChange<MutasiBerkasModel>,
    ILoadEntity<MutasiBerkasModel, IMutasiBerkasKey>
{
    MayBe<MutasiBerkasModel> GetLatestActiveByMr(string mrId);
    IEnumerable<MutasiBerkasModel> ListByTrsBuat(string trsBuatId);
}
