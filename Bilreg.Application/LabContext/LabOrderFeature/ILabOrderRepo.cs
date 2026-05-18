using Bilreg.Domain.LabContext.LabOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature;

public interface ILabOrderRepo :
    ISaveChange<LabOrderModel>,
    ILoadEntity<LabOrderModel, ILabOrderKey>,
    IDeleteEntity<ILabOrderKey>
{
}
