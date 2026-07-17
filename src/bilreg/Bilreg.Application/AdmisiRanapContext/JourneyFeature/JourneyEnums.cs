namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Release 1 journey projection version. Bump only for additive/breaking contract changes.
/// </summary>
public static class JourneyProjectionVersions
{
    public const int Release1 = 1;
}

public enum JourneyOriginKind
{
    OpnameRequest = 0,
    Reservation = 1,
    DirectOrLegacyAdmission = 2
}

/// <summary>
/// Release 1 operational stages. <see cref="Completed"/> is retained for forward compatibility
/// but is unreachable: no authoritative application process owns <c>Admission.Completed</c>.
/// <see cref="InWard"/> is deferred to Release 2 (Ward Management).
/// </summary>
public enum JourneyOperationalStage
{
    RegistrationRequired = 0,
    HandoverRequired = 1,
    WardAcceptanceRequired = 2,
    HandoverAccepted = 3,
    PlacementStatusUnavailable = 4,
    Completed = 5,
    Cancelled = 6,
    NeedsReconciliation = 7,
    InWard = 8
}

public enum JourneyOwnerDomain
{
    Admisi = 0,
    Ward = 1,
    SystemSupport = 2,
    None = 3
}

public enum JourneyPlacementAvailabilityStatus
{
    Unavailable = 0
}

public enum JourneyPlacementUnavailableReasonCode
{
    WardManagementNotImplemented = 0
}

public enum JourneyAttentionFlag
{
    WaitingOverThreshold = 0,
    ReservationOverdue = 1
}

public enum JourneyTimelineEventKind
{
    OpnameRequested = 0,
    OpnameCancelled = 1,
    OpnameFulfilled = 2,
    ReservationCreated = 3,
    ReservationMaintained = 4,
    ReservationCancelled = 5,
    ReservationRealized = 6,
    AdmissionRegistered = 7,
    AdmissionUpdated = 8,
    AdmissionCancelled = 9,
    WaitingListCreated = 10,
    WaitingListAccepted = 11,
    WaitingListClosed = 12,
    WaitingListCancelled = 13
}

public enum JourneyActionCode
{
    CompleteRegistration = 0,
    CreateAccommodationHandover = 1,
    WardAcceptHandover = 2,
    AwaitWardPlacement = 3,
    AwaitPlacementVisibility = 4,
    InvestigateReconciliation = 5,
    CancelOpnameRequest = 6,
    CancelReservation = 7,
    CancelAdmission = 8,
    UpdateAdmission = 9
}

/// <summary>
/// Structured codes for invalid or contradictory Release 1 relationships.
/// </summary>
public static class JourneyReconciliationCodes
{
    public const string AdmissionHasBothSources = "ADMISSION_HAS_BOTH_SOURCES";
    public const string MultipleAdmissionsForSource = "MULTIPLE_ADMISSIONS_FOR_SOURCE";
    public const string FulfilledSourceWithoutAdmission = "FULFILLED_SOURCE_WITHOUT_ADMISSION";
    public const string SourceFulfillmentRegIdMismatch = "SOURCE_FULFILLMENT_REGID_MISMATCH";
    public const string MultipleActiveWaitingLists = "MULTIPLE_ACTIVE_WAITING_LISTS";
    public const string WaitingListRegIdMismatch = "WAITING_LIST_REGID_MISMATCH";
    public const string OrphanWaitingList = "ORPHAN_WAITING_LIST";
    public const string OrphanRegistration = "ORPHAN_REGISTRATION";
    public const string ActiveWaitingListOnCancelledAdmission = "ACTIVE_WAITING_LIST_ON_CANCELLED_ADMISSION";
    public const string ActiveAdmissionOnCancelledSource = "ACTIVE_ADMISSION_ON_CANCELLED_SOURCE";
    public const string AdmissionCompletedWithoutAuthoritativeProcess =
        "ADMISSION_COMPLETED_WITHOUT_AUTHORITATIVE_PROCESS";
    public const string InternallyContradictoryState = "INTERNALLY_CONTRADICTORY_STATE";
}
