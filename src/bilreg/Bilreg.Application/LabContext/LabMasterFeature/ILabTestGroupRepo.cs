using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabTestGroupRepo :
    ISaveChange<LabTestGroupModel>,
    ILoadEntity<LabTestGroupModel, ILabTestGroupKey>,
    IListData<LabTestGroupModel>
{
    IEnumerable<LabTestGroupModel> ListData(string? search);
}
