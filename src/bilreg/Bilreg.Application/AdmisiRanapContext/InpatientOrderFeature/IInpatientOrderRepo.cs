using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;

public interface IInpatientOrderRepo :
    ISaveChange<InpatientOrderModel>,
    ILoadEntity<InpatientOrderModel, IInpatientOrderKey>
{
    MayBe<InpatientOrderModel> GetByKey(IInpatientOrderKey key);
    MayBe<InpatientOrderModel> GetByKey(string inpatientOrderId);
    IEnumerable<InpatientOrderModel> ListActive();
    IEnumerable<InpatientOrderModel> ListData(InpatientOrderListFilter filter);
    MayBe<InpatientOrderModel> GetByExternalReference(string externalReferenceId, string externalSystemSource);
}

public record InpatientOrderListFilter(
    InpatientOrderStatusEnum? Status = null,
    OrderSourceEnum? OrderSource = null,
    DateTime? PlannedFrom = null,
    DateTime? PlannedTo = null);
