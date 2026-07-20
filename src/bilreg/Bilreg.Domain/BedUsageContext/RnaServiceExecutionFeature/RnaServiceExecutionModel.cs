using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record RnaServiceExecutionModel : IRnaServiceExecutionKey
{
    private const string ID_PREFIX = "RSE";
    internal static readonly DateTime EmptyDate =
        new(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly List<ServiceWorkSourceRevisionType> _listSourceRevision;
    private readonly List<ServiceExecutionFactType> _listExecutionFact;
    private readonly List<ExecutionCorrectionModel> _listCorrection;
    private readonly List<ExecutionDeliveryReferenceType> _listDeliveryReference;

    public RnaServiceExecutionModel(
        string serviceExecutionId,
        string registrationId,
        string patientId,
        string careContextId,
        string responsibleWardId,
        ExecutionSourceEnum executionSource,
        string clinicalOrderId,
        string orderOccurrenceId,
        string fulfilmentObligationId,
        string sourceContext,
        string sourceFactId,
        int sourceRevision,
        string assignedPerformerId,
        RnaServiceWorkStatusEnum workStatus,
        ExecutionAuthorityType? executionAuthority,
        int version,
        IEnumerable<ServiceWorkSourceRevisionType> listSourceRevision,
        IEnumerable<ServiceExecutionFactType> listExecutionFact,
        IEnumerable<ExecutionCorrectionModel> listCorrection,
        IEnumerable<ExecutionDeliveryReferenceType> listDeliveryReference)
    {
        ServiceExecutionId = serviceExecutionId;
        RegistrationId = registrationId;
        PatientId = patientId;
        CareContextId = careContextId;
        ResponsibleWardId = responsibleWardId;
        ExecutionSource = executionSource;
        ClinicalOrderId = clinicalOrderId;
        OrderOccurrenceId = orderOccurrenceId;
        FulfilmentObligationId = fulfilmentObligationId;
        SourceContext = sourceContext;
        SourceFactId = sourceFactId;
        SourceRevision = sourceRevision;
        AssignedPerformerId = assignedPerformerId;
        WorkStatus = workStatus;
        ExecutionAuthority = executionAuthority;
        Version = version;
        _listSourceRevision = listSourceRevision?.ToList() ?? [];
        _listExecutionFact = listExecutionFact?.ToList() ?? [];
        _listCorrection = listCorrection?.ToList() ?? [];
        _listDeliveryReference = listDeliveryReference?.ToList() ?? [];
    }

    #region CREATION

    public static RnaServiceExecutionModel CreateOrdered(
        string registrationId,
        string patientId,
        string careContextId,
        string responsibleWardId,
        string clinicalOrderId,
        string? orderOccurrenceId,
        string fulfilmentObligationId,
        string sourceContext,
        string sourceFactId,
        int sourceRevision)
    {
        Guard.Against.NullOrWhiteSpace(registrationId);
        Guard.Against.NullOrWhiteSpace(patientId);
        Guard.Against.NullOrWhiteSpace(careContextId);
        Guard.Against.NullOrWhiteSpace(responsibleWardId);
        Guard.Against.NullOrWhiteSpace(clinicalOrderId);
        Guard.Against.NullOrWhiteSpace(fulfilmentObligationId);
        Guard.Against.NullOrWhiteSpace(sourceContext);
        Guard.Against.NullOrWhiteSpace(sourceFactId);
        if (sourceRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));

        return new RnaServiceExecutionModel(
            NunaId.New(ID_PREFIX), registrationId, patientId, careContextId, responsibleWardId,
            ExecutionSourceEnum.Ordered, clinicalOrderId, orderOccurrenceId ?? string.Empty,
            fulfilmentObligationId, sourceContext, sourceFactId, sourceRevision, string.Empty,
            RnaServiceWorkStatusEnum.Pending, null, 0, [], [], [], []);
    }

    public static RnaServiceExecutionModel CreateAdHocOrIndependent(
        string registrationId,
        string patientId,
        string careContextId,
        string responsibleWardId,
        ExecutionSourceEnum executionSource,
        ExecutionAuthorityType executionAuthority)
    {
        Guard.Against.NullOrWhiteSpace(registrationId);
        Guard.Against.NullOrWhiteSpace(patientId);
        Guard.Against.NullOrWhiteSpace(careContextId);
        Guard.Against.NullOrWhiteSpace(responsibleWardId);
        Guard.Against.Null(executionAuthority);
        if (executionSource is not ExecutionSourceEnum.AdHoc and not ExecutionSourceEnum.Independent)
            throw new ArgumentOutOfRangeException(nameof(executionSource));

        return new RnaServiceExecutionModel(
            NunaId.New(ID_PREFIX), registrationId, patientId, careContextId, responsibleWardId,
            executionSource, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0,
            string.Empty, RnaServiceWorkStatusEnum.Pending, executionAuthority, 0, [], [], [], []);
    }

    public static RnaServiceExecutionModel Default => new(
        "-", "-", "-", "-", "-", ExecutionSourceEnum.Ordered,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty,
        RnaServiceWorkStatusEnum.Pending, null, 0, [], [], [], []);

    public static IRnaServiceExecutionKey Key(string serviceExecutionId)
    {
        Guard.Against.NullOrWhiteSpace(serviceExecutionId);
        return Default with { ServiceExecutionId = serviceExecutionId };
    }

    #endregion

    #region PROPERTIES

    public string ServiceExecutionId { get; init; }
    public string RegistrationId { get; init; }
    public string PatientId { get; init; }
    public string CareContextId { get; init; }
    public string ResponsibleWardId { get; init; }
    public ExecutionSourceEnum ExecutionSource { get; init; }
    public string ClinicalOrderId { get; init; }
    public string OrderOccurrenceId { get; init; }
    public string FulfilmentObligationId { get; init; }
    public string SourceContext { get; init; }
    public string SourceFactId { get; init; }
    public int SourceRevision { get; init; }
    public string AssignedPerformerId { get; init; }
    public RnaServiceWorkStatusEnum WorkStatus { get; init; }
    public ExecutionAuthorityType? ExecutionAuthority { get; init; }
    public int Version { get; init; }
    public IEnumerable<ServiceWorkSourceRevisionType> ListSourceRevision => _listSourceRevision.AsReadOnly();
    public IEnumerable<ServiceExecutionFactType> ListExecutionFact => _listExecutionFact.AsReadOnly();
    public IEnumerable<ExecutionCorrectionModel> ListCorrection => _listCorrection.AsReadOnly();
    public IEnumerable<ExecutionDeliveryReferenceType> ListDeliveryReference => _listDeliveryReference.AsReadOnly();
    public ServiceExecutionFactType? CurrentExecutionFact =>
        WorkStatus == RnaServiceWorkStatusEnum.EnteredInError
            ? null
            : _listExecutionFact.OrderByDescending(x => x.ExecutionRevision).FirstOrDefault();

    public bool IsOrdered => ExecutionSource == ExecutionSourceEnum.Ordered;
    public bool IsExecuted => WorkStatus is RnaServiceWorkStatusEnum.Executed or RnaServiceWorkStatusEnum.EnteredInError;

    #endregion

    #region BEHAVIOUR

    public RnaServiceExecutionModel AssignPerformer(string performerId)
    {
        Guard.Against.NullOrWhiteSpace(performerId);
        EnsurePendingOrAssigned();
        return WithState(assignedPerformerId: performerId, workStatus: RnaServiceWorkStatusEnum.Assigned);
    }

    public RnaServiceExecutionModel ApplySourceRevision(
        string sourceContext,
        string sourceFactId,
        int sourceRevision,
        SourceRevisionKindEnum revisionKind,
        DateTime effectiveAt,
        DateTime recordedAt,
        string actorId,
        string? reason)
    {
        if (!IsOrdered)
            throw new InvalidOperationException("Source revision hanya berlaku untuk ordered service work.");
        if (!string.Equals(sourceContext, SourceContext, StringComparison.Ordinal))
            throw new InvalidOperationException("Source context revision tidak sesuai dengan aggregate.");
        if (sourceRevision <= SourceRevision)
            throw new InvalidOperationException("Source revision harus lebih baru dari revision yang telah diterapkan.");

        var revision = ServiceWorkSourceRevisionType.Create(
            sourceContext, sourceFactId, sourceRevision, revisionKind, effectiveAt, recordedAt, actorId, reason);
        var status = WorkStatus;
        if (revisionKind is SourceRevisionKindEnum.Cancelled or SourceRevisionKindEnum.Discontinued &&
            WorkStatus is RnaServiceWorkStatusEnum.Pending or RnaServiceWorkStatusEnum.Assigned)
            status = RnaServiceWorkStatusEnum.Cancelled;

        return WithState(
            sourceFactId: sourceFactId,
            sourceRevision: sourceRevision,
            workStatus: status,
            listSourceRevision: _listSourceRevision.Append(revision));
    }

    public RnaServiceExecutionModel WithdrawUnexecutedWork()
    {
        EnsurePendingOrAssigned();
        return WithState(workStatus: RnaServiceWorkStatusEnum.Withdrawn);
    }

    public RnaServiceExecutionModel RecordExecution(
        BillableClassificationEnum billableClassification,
        TarifServiceReff? tarifService,
        string? nonBillableDescription,
        string performerId,
        DateTime performedAt,
        DateTime recordedAt,
        string recorderActorId,
        string? lateEntryReason)
    {
        EnsurePendingOrAssigned();
        if (_listExecutionFact.Count != 0)
            throw new InvalidOperationException("Service execution fact awal sudah pernah dicatat.");

        var fact = ServiceExecutionFactType.Record(
            1, billableClassification, tarifService, nonBillableDescription, performerId,
            performedAt, recordedAt, recorderActorId, lateEntryReason);
        return WithState(
            assignedPerformerId: performerId,
            workStatus: RnaServiceWorkStatusEnum.Executed,
            listExecutionFact: _listExecutionFact.Append(fact));
    }

    public RnaServiceExecutionModel AssociateSubsequentAuthorization(
        SubsequentAuthorizationStatusEnum status)
    {
        if (ExecutionAuthority is null)
            throw new InvalidOperationException("Execution ini tidak memiliki authority record.");
        return WithState(executionAuthority: ExecutionAuthority.AssociateStatus(status));
    }

    public RnaServiceExecutionModel CorrectExecution(
        ExecutionCorrectionKindEnum correctionKind,
        BillableClassificationEnum billableClassification,
        TarifServiceReff? tarifService,
        string? nonBillableDescription,
        string performerId,
        DateTime performedAt,
        DateTime recordedAt,
        string recorderActorId,
        string? lateEntryReason,
        string correctionReason,
        string correctingActorId,
        string? secondReviewerId,
        string? evidenceReference,
        DateTime correctedAt)
    {
        if (correctionKind == ExecutionCorrectionKindEnum.EnteredInError)
            throw new ArgumentException("Gunakan MarkEnteredInError untuk koreksi jenis EnteredInError.", nameof(correctionKind));
        var current = RequireCurrentExecutionFact();
        EnsureCorrectionReview(correctionKind, correctingActorId, secondReviewerId);
        var replacement = ServiceExecutionFactType.Record(
            current.ExecutionRevision + 1, billableClassification, tarifService, nonBillableDescription,
            performerId, performedAt, recordedAt, recorderActorId, lateEntryReason);
        var correction = ExecutionCorrectionModel.Create(
            _listExecutionFact.First().ServiceExecutionFactId, current.ExecutionRevision,
            replacement.ExecutionRevision, correctionKind, replacement.ServiceExecutionFactId,
            correctionReason, correctingActorId, secondReviewerId, evidenceReference, correctedAt, recordedAt);

        return WithState(
            assignedPerformerId: performerId,
            listExecutionFact: _listExecutionFact.Append(replacement),
            listCorrection: _listCorrection.Append(correction));
    }

    public RnaServiceExecutionModel MarkEnteredInError(
        string correctionReason,
        string correctingActorId,
        string secondReviewerId,
        string evidenceReference,
        DateTime correctedAt,
        DateTime recordedAt = default)
    {
        var current = RequireCurrentExecutionFact();
        EnsureCorrectionReview(ExecutionCorrectionKindEnum.EnteredInError, correctingActorId, secondReviewerId);
        var correction = ExecutionCorrectionModel.Create(
            _listExecutionFact.First().ServiceExecutionFactId, current.ExecutionRevision,
            current.ExecutionRevision + 1, ExecutionCorrectionKindEnum.EnteredInError, null,
            correctionReason, correctingActorId, secondReviewerId, evidenceReference, correctedAt, recordedAt);

        return WithState(
            workStatus: RnaServiceWorkStatusEnum.EnteredInError,
            listCorrection: _listCorrection.Append(correction));
    }

    #endregion

    #region HELPERS

    private void EnsurePendingOrAssigned()
    {
        if (WorkStatus is not RnaServiceWorkStatusEnum.Pending and not RnaServiceWorkStatusEnum.Assigned)
            throw new InvalidOperationException(
                $"Service execution {ServiceExecutionId} tidak dapat diubah pada status {WorkStatus}.");
    }

    private ServiceExecutionFactType RequireCurrentExecutionFact() =>
        CurrentExecutionFact ?? throw new InvalidOperationException("Tidak ada execution fact aktif yang dapat dikoreksi.");

    private static void EnsureCorrectionReview(
        ExecutionCorrectionKindEnum correctionKind,
        string correctingActorId,
        string? secondReviewerId)
    {
        Guard.Against.NullOrWhiteSpace(correctingActorId);
        if (correctionKind is ExecutionCorrectionKindEnum.Replaced or ExecutionCorrectionKindEnum.EnteredInError)
        {
            Guard.Against.NullOrWhiteSpace(secondReviewerId);
            if (string.Equals(correctingActorId, secondReviewerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Second reviewer harus independen dari correcting actor.");
        }
    }

    private RnaServiceExecutionModel WithState(
        string? sourceFactId = null,
        int? sourceRevision = null,
        string? assignedPerformerId = null,
        RnaServiceWorkStatusEnum? workStatus = null,
        ExecutionAuthorityType? executionAuthority = null,
        IEnumerable<ServiceWorkSourceRevisionType>? listSourceRevision = null,
        IEnumerable<ServiceExecutionFactType>? listExecutionFact = null,
        IEnumerable<ExecutionCorrectionModel>? listCorrection = null,
        IEnumerable<ExecutionDeliveryReferenceType>? listDeliveryReference = null)
        => new(
            ServiceExecutionId, RegistrationId, PatientId, CareContextId, ResponsibleWardId,
            ExecutionSource, ClinicalOrderId, OrderOccurrenceId, FulfilmentObligationId, SourceContext,
            sourceFactId ?? SourceFactId, sourceRevision ?? SourceRevision,
            assignedPerformerId ?? AssignedPerformerId, workStatus ?? WorkStatus,
            executionAuthority ?? ExecutionAuthority, Version + 1,
            listSourceRevision ?? _listSourceRevision,
            listExecutionFact ?? _listExecutionFact,
            listCorrection ?? _listCorrection,
            listDeliveryReference ?? _listDeliveryReference);

    #endregion
}

public interface IRnaServiceExecutionKey
{
    string ServiceExecutionId { get; }
}
