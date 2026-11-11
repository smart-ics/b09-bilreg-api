using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabTestGroupDal :
    IInsert<LabTestGroupDto>,
    IUpdate<LabTestGroupDto>,
    IDelete<ILabTestGroupKey>,
    IGetData<LabTestGroupDto?, ILabTestGroupKey>,
    IListData<LabTestGroupDto>
{
    IEnumerable<LabTestGroupDto> ListData(string? search);
}
