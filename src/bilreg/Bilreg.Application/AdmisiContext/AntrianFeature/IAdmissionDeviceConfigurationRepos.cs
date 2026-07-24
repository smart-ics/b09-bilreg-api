using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionWorkstationRepo :
    ISaveChange<AdmissionWorkstationModel>,
    ILoadEntity<AdmissionWorkstationModel, IAdmissionWorkstationKey>
{
    IReadOnlyList<AdmissionWorkstationModel> ListAll();
    AdmissionWorkstationModel? FindActiveByLoketKey(string loketKey, string? excludeWorkstationKey = null);
}

public interface IAdmissionQueueDisplayRepo :
    ISaveChange<AdmissionQueueDisplayModel>,
    ILoadEntity<AdmissionQueueDisplayModel, IAdmissionQueueDisplayKey>
{
    IReadOnlyList<AdmissionQueueDisplayModel> ListAll();
}
