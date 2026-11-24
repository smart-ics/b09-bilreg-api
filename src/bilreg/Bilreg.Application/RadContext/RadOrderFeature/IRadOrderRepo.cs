using Bilreg.Domain.RadContext.RadOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public interface IRadOrderRepo :
    ISaveChange<RadOrderModel>,
    ILoadEntity<RadOrderModel, IRadOrderKey>
{
    MayBe<RadOrderModel> LoadByEmrOrderId(string emrOrderId);
    IEnumerable<RadOrderModel> ListByRegId(string regId);
}
