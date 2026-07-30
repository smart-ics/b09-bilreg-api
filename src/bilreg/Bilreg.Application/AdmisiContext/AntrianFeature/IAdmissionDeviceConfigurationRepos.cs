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

public interface IAdmissionQueueKioskRepo :
    ISaveChange<AdmissionQueueKioskModel>,
    ILoadEntity<AdmissionQueueKioskModel, IAdmissionQueueKioskKey>
{
    IReadOnlyList<AdmissionQueueKioskModel> ListAll();
}

public sealed record AdmissionConfigurationAuditItem(
    string AuditId,
    DateTime EventTime,
    string UserId,
    string ActionType,
    string EntityName,
    string EntityId,
    string? Reason,
    string? OriginalDataJson,
    string? CorrelationId);

public interface IAdmissionConfigurationAuditReader
{
    IReadOnlyList<AdmissionConfigurationAuditItem> List(int page, int pageSize, out int totalCount);
}
