using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

/// <summary>Explicit, lock-aware persistence boundary for the coordinated cancellation use case.</summary>
public interface ICoordinatedCancellationRepo
{
    CoordinatedCancellationLedger? LockLedger(string requestId);
    bool TryStartLedger(CoordinatedCancellationLedger ledger);
    CoordinatedCancellationState LockState(string regId);
    int CancelWaitingList(CoordinatedCancellationState state, string userId, DateTime now);
    int EndDoctorAssignments(string regId, DateOnly effectiveDate);
    int VoidRegistration(string regId, string userId, DateTime now);
    int DeleteRegAktif(string regId);
    int RestoreSource(CoordinatedCancellationState state, string userId, DateTime now);
    int CancelAdmission(CoordinatedCancellationState state, DateTime? expectedUpdatedAt, string userId, DateTime now);
    int CompleteLedger(string requestId, string responseJson, DateTime now);
}

public sealed record CoordinatedCancellationLedger(
    string RequestId,
    string Fingerprint,
    string RegId,
    string Lifecycle,
    string ResponseJson,
    DateTime CreatedAt,
    DateTime CompletedAt,
    string CorrelationId);

public sealed record CoordinatedCancellationState(
    string RegId,
    AdmissionStatusEnum AdmissionStatus,
    DateTime AdmissionUpdatedAt,
    AdmissionSourceEnum AdmissionSource,
    string OpnameRequestId,
    string ReservationId,
    bool RegistrationExists,
    bool RegInapExists,
    int ActiveDoctorAssignments,
    string? WaitingListId,
    WaitingListStatusEnum? WaitingListStatus,
    int RegAktifCount,
    string? SourceId,
    string? SourceKind,
    bool SourceOwnedAndCancellable,
    bool IsCoherentlyCancelled,
    object AuditSnapshot,
    bool AdmissionExists = true,
    CoordinatedCancellationAuditSnapshots? AuditSnapshots = null);

/// <summary>
/// Immutable, entity-scoped pre-change images captured while the cancellation
/// locks are held.  A cancellation audit must never reuse a catch-all state
/// image: it would make a RegAktif DELETE or doctor-history VOID impossible to
/// reconstruct independently.
/// </summary>
public sealed record CoordinatedCancellationAuditSnapshots(
    object? Admission,
    object? Registration,
    object? RegInapAndDoctorHistory,
    object? RegAktif,
    object? WaitingList,
    object? Source);

public static class CoordinatedCancellationErrorCode
{
    public const string RegistrationHasBillingItems = "REGISTRATION_HAS_BILLING_ITEMS";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string SourceStateMismatch = "SOURCE_STATE_MISMATCH";
    public const string RequestIdReused = "REQUEST_ID_REUSED";
    public const string StateInconsistent = "COORDINATED_STATE_INCONSISTENT";
}

public sealed class CoordinatedCancellationException : InvalidOperationException
{
    public CoordinatedCancellationException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
