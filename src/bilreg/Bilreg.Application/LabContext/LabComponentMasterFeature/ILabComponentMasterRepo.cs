using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabComponentMasterFeature;

public interface ILabComponentMasterRepo :
    ILoadEntity<LabComponentMasterModel, ILabComponentMasterKey>,
    IListData<LabComponentMasterModel>,
    IListData<LabComponentMasterModel, LabComponentMasterListFilter>
{
    MayBe<LabComponentMasterModel> LoadByComponentCode(string componentCode);
}
