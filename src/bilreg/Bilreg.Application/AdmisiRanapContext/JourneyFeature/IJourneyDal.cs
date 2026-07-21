namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

public interface IJourneyDal
{
    JourneyListResult List(JourneyListFilter filter, DateTime businessNow);

    JourneyDetailWorkspace? GetByJourneyId(string journeyId, DateTime businessNow);

    JourneyLegacyResolution? ResolveLegacyRecord(string recordType, string recordId, DateTime businessNow);

    /// <summary>Diagnostics from the most recent <see cref="List"/> call (round-trips and phase timings).</summary>
    JourneyListDiagnostics? LastListDiagnostics { get; }
}

public enum JourneyListScope
{
    Active = 0,
    History = 1
}

public sealed record JourneyListFilter(
    JourneyListScope Scope = JourneyListScope.Active,
    JourneyOperationalStage? Stage = null,
    string? SearchTerm = null,
    string? DokterId = null,
    string? BangsalId = null,
    string? KelasId = null,
    string? TipeJaminanId = null,
    int? Priority = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Cursor = null,
    int PageSize = 50);

public sealed record JourneyIdName(string? Id, string? Name);

public sealed record JourneyListItem(
    string JourneyId,
    string? RegId,
    JourneyOriginKind OriginKind,
    JourneyPatientSummary? Patient,
    JourneyOperationalStage Stage,
    JourneyCurrentCondition CurrentCondition,
    JourneyNextTask? NextTask,
    JourneyIdName? CareClass,
    JourneyIdName? TargetBangsal,
    int? Priority,
    DateTime? WaitingSince,
    long? WaitingDurationSeconds,
    JourneyIdName? Doctor,
    JourneyIdName? Guarantor,
    IReadOnlyList<JourneyAttentionFlag> AttentionFlags,
    IReadOnlyList<JourneyReconciliationIssue> ReconciliationIssues,
    DateTime SortAt,
    int ProjectionVersion,
    DateTime AsOf);

public sealed record JourneyStageFacet(JourneyOperationalStage Stage, int Count);

public sealed record JourneyListResult(
    IReadOnlyList<JourneyListItem> Items,
    IReadOnlyList<JourneyStageFacet> StageFacets,
    int TotalMatches,
    string? NextCursor,
    DateTime AsOf,
    int ProjectionVersion);

public sealed record JourneyPatientContactInfo(
    string PatientId,
    string MedicalRecordNumber,
    string DisplayName,
    string? Sex,
    DateOnly? BirthDate,
    string? AgeDisplay,
    string? ContactSummary,
    string? AddressSummary);

public sealed record JourneyGuarantorCareClassInfo(
    JourneyIdName? Guarantor,
    JourneyIdName? CareClass,
    string? PolicySummary);

public sealed record JourneyClinicalInfo(
    string? AdmissionReasonOrNotes,
    string? ProcedureSummary,
    JourneyIdName? Dpjp,
    JourneyIdName? ReferringDoctor,
    string? ReferralOrSourceSummary);

public sealed record JourneyHandoverInfo(
    JourneyAccommodationHandoverSummary Summary,
    JourneyIdName? TargetBangsal,
    int? Priority);

public sealed record JourneyDetailInformation(
    JourneyPatientContactInfo? PatientAndContact,
    JourneyGuarantorCareClassInfo? GuarantorAndCareClass,
    JourneyClinicalInfo? Clinical,
    JourneyPlacementAvailability PlacementAvailability,
    JourneyHandoverInfo? Handover);

public sealed record JourneyDetailWorkspace(
    string JourneyId,
    JourneyIdentity Identity,
    JourneyPatientSummary? PatientEpisodeHeader,
    JourneyOperationalStage Stage,
    JourneyCurrentCondition CurrentCondition,
    JourneyNextTask? NextTask,
    /// <summary>Stage/ownership candidates from B1 (not final command eligibility).</summary>
    IReadOnlyList<JourneyAllowedAction> CandidateAdmisiActions,
    /// <summary>Candidates after currently implemented command dependency checks (e.g. Tata Rekening billing).</summary>
    IReadOnlyList<JourneyAllowedAction> AllowedAdmisiActions,
    IReadOnlyList<JourneyTimelineEvent> Timeline,
    JourneyDetailInformation Information,
    JourneySystemAuditReferences SystemAudit,
    IReadOnlyList<JourneyReconciliationIssue> ReconciliationIssues,
    DateTime AsOf,
    int ProjectionVersion);

/// <summary>Optional diagnostics captured by the last <see cref="IJourneyDal.List"/> call (tests / profiling).</summary>
public sealed record JourneyListDiagnostics(
    int DatabaseRoundTrips,
    long FacetPageSqlMs,
    long BatchHydrationMs,
    long ResolverEnrichmentMs,
    long TotalListMs,
    int PageItemCount);

/// <summary>
/// Compatibility resolution for a legacy aggregate link.  Ambiguous relationships are returned
/// explicitly so a client can show reconciliation instead of selecting an inferred episode.
/// </summary>
public sealed record JourneyLegacyResolution(
    string? JourneyId,
    bool RequiresReconciliation,
    IReadOnlyList<JourneyReconciliationIssue> ReconciliationIssues);
