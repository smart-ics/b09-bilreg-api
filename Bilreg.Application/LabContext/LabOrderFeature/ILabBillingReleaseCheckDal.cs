using Bilreg.Domain.LabContext.LabOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature;

public interface ILabBillingReleaseCheckDal
{
    void Insert(BillingReleaseCheckModel check);

    MayBe<BillingReleaseCheckModel> GetLastByOrderId(string orderId);
}
