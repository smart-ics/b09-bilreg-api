using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

public interface IAdmissionRepo :
    ISaveChange<AdmissionModel>,
    ILoadEntity<AdmissionModel, IRegKey>
{
    IEnumerable<AdmissionModel> ListData(AdmissionListFilter filter);
}

public record AdmissionListFilter(
    AdmissionStatusEnum? Status = null,
    string? PasienId = null);
