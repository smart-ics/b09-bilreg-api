using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;

public interface IOpnameRequestRepo :
    ISaveChange<OpnameRequestModel>,
    ILoadEntity<OpnameRequestModel, IOpnameRequestKey>
{
    IEnumerable<OpnameRequestModel> ListData(OpnameRequestListFilter filter);
}

public record OpnameRequestListFilter(OpnameRequestStatusEnum? Status = null);
