using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;

public interface IOpnameRequestRepo :
    ISaveChange<OpnameRequestModel>,
    ILoadEntity<OpnameRequestModel, IOpnameRequestKey>
{
    IEnumerable<OpnameRequestModel> ListData(OpnameRequestListFilter filter);
    MayBe<OpnameRequestModel> GetByEmrOrder(string emrOrderId);
}

public record OpnameRequestListFilter(OpnameRequestStatusEnum? Status = null);
