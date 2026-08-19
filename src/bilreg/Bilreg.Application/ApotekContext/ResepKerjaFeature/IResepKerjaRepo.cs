using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.ResepKerjaFeature;

public interface IResepKerjaRepo :
    ISaveChange<ResepKerjaModel>,
    ILoadEntity<ResepKerjaModel, IResepKerjaKey>
{
    MayBe<ResepKerjaModel> LoadBySource(ResepKerjaSourceKindEnum sourceKind, string sourceResepId);
}
