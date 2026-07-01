using Bilreg.Domain.LabContext.LabOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature;

public interface ILabOrderRepo :
    ISaveChange<LabOrderModel>,
    ILoadEntity<LabOrderModel, ILabOrderKey>,
    IDeleteEntity<ILabOrderKey>
{
    MayBe<LabOrderModel> LoadByEmrOrderId(string emrOrderId);
}
