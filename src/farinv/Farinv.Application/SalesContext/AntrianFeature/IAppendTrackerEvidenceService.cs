using Nuna.Lib.CleanArchHelper;

namespace Farinv.Application.SalesContext.AntrianFeature;

public interface IAppendTrackerEvidenceService : INunaService<AppendPharmacyEvidenceRequest, AppendPharmacyEvidenceRequest>
{
}

public record AppendPharmacyEvidenceRequest(
    string PasienTrackerId,
    string EventName,
    string ReffId,
    DateTime OccurredAt);
