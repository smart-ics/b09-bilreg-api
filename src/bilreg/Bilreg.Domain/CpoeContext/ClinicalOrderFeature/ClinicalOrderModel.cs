using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.CpoeContext.ClinicalOrderFeature;

public interface IClinicalOrderKey
{
    string ClinicalOrderId { get; }
}

public record ClinicalOrderReff(string ClinicalOrderId, string OrderTypeName);

public record ClinicalOrderModel : IClinicalOrderKey
{
    private const string IdPrefix = "CPO";
    private readonly List<OrderOccurrenceModel> _listOccurrence;
    private readonly List<OrderHistoryEntryModel> _listHistory;

    public ClinicalOrderModel(
        string clinicalOrderId,
        string patientId,
        string regId,
        string orderingClinicianId,
        OrderTypeType orderType,
        OrderSpecificationType orderSpecification,
        ClinicalOrderStatusEnum clinicalOrderStatus,
        IEnumerable<OrderOccurrenceModel>? listOccurrence,
        IEnumerable<OrderHistoryEntryModel>? listHistory)
    {
        ClinicalOrderId = clinicalOrderId;
        PatientId = patientId;
        RegId = regId;
        OrderingClinicianId = orderingClinicianId;
        OrderType = orderType;
        OrderSpecification = orderSpecification;
        ClinicalOrderStatus = clinicalOrderStatus;
        _listOccurrence = listOccurrence?.ToList() ?? [];
        _listHistory = listHistory?.ToList() ?? [];
    }

    #region CREATION

    public static ClinicalOrderModel Default => new(
        "-", "-", "-", "-", OrderTypeType.Default, OrderSpecificationType.Default,
        ClinicalOrderStatusEnum.Active, [], []);

    public static IClinicalOrderKey Key(string clinicalOrderId)
    {
        Guard.Against.NullOrWhiteSpace(clinicalOrderId);
        return Default with { ClinicalOrderId = clinicalOrderId };
    }

    public static ClinicalOrderModel Load(
        string clinicalOrderId,
        string patientId,
        string regId,
        string orderingClinicianId,
        OrderTypeType orderType,
        OrderSpecificationType orderSpecification,
        ClinicalOrderStatusEnum clinicalOrderStatus,
        IEnumerable<OrderOccurrenceModel> listOccurrence,
        IEnumerable<OrderHistoryEntryModel> listHistory)
        => new(
            clinicalOrderId, patientId, regId, orderingClinicianId, orderType, orderSpecification,
            clinicalOrderStatus, listOccurrence, listHistory);

    public static ClinicalOrderModel Create(
        string patientId,
        string regId,
        string orderingClinicianId,
        OrderTypeType orderType,
        OrderSpecificationType orderSpecification,
        IEnumerable<OrderOccurrencePlanType> occurrencePlan,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(patientId);
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(orderingClinicianId);
        Guard.Against.Null(orderType);
        Guard.Against.Null(orderSpecification);
        if (createdAt == default)
            throw new ArgumentException("Created time wajib diisi.", nameof(createdAt));

        var occurrences = occurrencePlan?.Select(OrderOccurrenceModel.Create).ToList() ?? [];
        Guard.Against.NullOrEmpty(occurrences, nameof(occurrencePlan));

        var history = OrderHistoryEntryModel.Create(
            OrderHistoryActionEnum.Created, orderingClinicianId, createdAt, "Order created", string.Empty,
            $"{orderType.OrderTypeId}: {orderSpecification.RequestedWork}");
        return new ClinicalOrderModel(
            NunaId.New(IdPrefix), patientId, regId, orderingClinicianId, orderType, orderSpecification,
            ClinicalOrderStatusEnum.Active, occurrences, [history]);
    }

    #endregion

    #region PROPERTIES

    public string ClinicalOrderId { get; init; }
    public string PatientId { get; init; }
    public string RegId { get; init; }
    public string OrderingClinicianId { get; init; }
    public OrderTypeType OrderType { get; init; }
    public OrderSpecificationType OrderSpecification { get; init; }
    public ClinicalOrderStatusEnum ClinicalOrderStatus { get; init; }
    public IEnumerable<OrderOccurrenceModel> ListOccurrence => _listOccurrence.AsReadOnly();
    public IEnumerable<OrderHistoryEntryModel> ListHistory => _listHistory.AsReadOnly();
    public bool IsScheduled => _listOccurrence.Count > 1;
    public CompletionProgressType CompletionProgress => new(
        _listOccurrence.Count,
        _listOccurrence.Count(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Active),
        _listOccurrence.Count(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Completed),
        _listOccurrence.Count(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.NotPerformed),
        _listOccurrence.Count(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Cancelled));

    public ClinicalOrderReff ToReff() => new(ClinicalOrderId, OrderType.OrderTypeName);

    #endregion

    #region BEHAVIOUR

    public ClinicalOrderModel ModifyIntent(
        OrderTypeType orderType,
        OrderSpecificationType orderSpecification,
        string actorId,
        DateTime occurredAt,
        string reason)
    {
        Guard.Against.Null(orderType);
        Guard.Against.Null(orderSpecification);
        EnsureEveryOccurrenceIsActive();
        EnsureReason(reason);
        return WithState(
            orderType: orderType,
            orderSpecification: orderSpecification,
            history: AppendHistory(OrderHistoryActionEnum.IntentModified, actorId, occurredAt, reason,
                DescribeIntent(OrderType, OrderSpecification), DescribeIntent(orderType, orderSpecification)));
    }

    public ClinicalOrderModel ReplaceOccurrencePlan(
        IEnumerable<OrderOccurrencePlanType> occurrencePlan,
        string actorId,
        DateTime occurredAt,
        string reason)
    {
        EnsureEveryOccurrenceIsActive();
        EnsureReason(reason);
        var plans = occurrencePlan?.ToList() ?? [];
        Guard.Against.NullOrEmpty(plans, nameof(occurrencePlan));
        var occurrences = plans.Select(OrderOccurrenceModel.Create).ToList();
        return WithState(
            occurrences: occurrences,
            history: AppendHistory(OrderHistoryActionEnum.OccurrencePlanModified, actorId, occurredAt, reason,
                DescribePlan(_listOccurrence), DescribePlan(occurrences)));
    }

    public ClinicalOrderModel ChangeOccurrenceDestination(
        string orderOccurrenceId,
        DestinationType destination,
        string actorId,
        DateTime occurredAt,
        string reason)
    {
        Guard.Against.NullOrWhiteSpace(orderOccurrenceId);
        Guard.Against.Null(destination);
        EnsureActive();
        EnsureReason(reason);
        var occurrence = FindOccurrence(orderOccurrenceId);
        var changed = occurrence.ChangeDestination(destination);
        return WithState(
            occurrences: ReplaceOccurrence(changed),
            history: AppendHistory(OrderHistoryActionEnum.OccurrenceDestinationChanged, actorId, occurredAt, reason,
                occurrence.Destination.DestinationId, destination.DestinationId));
    }

    public ClinicalOrderModel ChangeOccurrencePlannedExecutionTime(
        string orderOccurrenceId,
        DateTime plannedExecutionAt,
        string actorId,
        DateTime occurredAt,
        string reason)
    {
        Guard.Against.NullOrWhiteSpace(orderOccurrenceId);
        EnsureActive();
        EnsureReason(reason);
        var occurrence = FindOccurrence(orderOccurrenceId);
        var changed = occurrence.ChangePlannedExecutionAt(plannedExecutionAt);
        return WithState(
            occurrences: ReplaceOccurrence(changed),
            history: AppendHistory(OrderHistoryActionEnum.OccurrencePlannedExecutionTimeChanged, actorId, occurredAt, reason,
                occurrence.PlannedExecutionAt.ToString("O"), plannedExecutionAt.ToString("O")));
    }

    public ClinicalOrderModel RecordFulfilment(
        string orderOccurrenceId,
        FulfilmentEvidenceType fulfilmentEvidence)
    {
        Guard.Against.NullOrWhiteSpace(orderOccurrenceId);
        Guard.Against.Null(fulfilmentEvidence);
        EnsureActive();
        var occurrence = FindOccurrence(orderOccurrenceId);
        var finalised = occurrence.RecordFulfilment(fulfilmentEvidence);
        var action = fulfilmentEvidence.Outcome == FulfilmentOutcomeEnum.Completed
            ? OrderHistoryActionEnum.OccurrenceCompleted
            : OrderHistoryActionEnum.OccurrenceNotPerformed;
        var reason = fulfilmentEvidence.Outcome == FulfilmentOutcomeEnum.NotPerformed
            ? fulfilmentEvidence.NotPerformedReason
            : fulfilmentEvidence.Narrative;
        var occurrences = ReplaceOccurrence(finalised);
        return WithState(
            clinicalOrderStatus: DeriveFulfilmentStatus(occurrences),
            occurrences: occurrences,
            history: AppendHistory(action, fulfilmentEvidence.AttesterId, fulfilmentEvidence.RecordedAt, reason,
                OrderOccurrenceStatusEnum.Active.ToString(), finalised.OrderOccurrenceStatus.ToString()));
    }

    public ClinicalOrderModel Cancel(string actorId, DateTime occurredAt, string reason)
    {
        EnsureActive();
        EnsureReason(reason);
        var occurrences = _listOccurrence
            .Select(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Active ? x.Cancel() : x)
            .ToList();
        return WithState(
            clinicalOrderStatus: ClinicalOrderStatusEnum.Cancelled,
            occurrences: occurrences,
            history: AppendHistory(OrderHistoryActionEnum.Cancelled, actorId, occurredAt, reason,
                ClinicalOrderStatusEnum.Active.ToString(), ClinicalOrderStatusEnum.Cancelled.ToString()));
    }

    #endregion

    #region HELPERS

    private ClinicalOrderModel WithState(
        OrderTypeType? orderType = null,
        OrderSpecificationType? orderSpecification = null,
        ClinicalOrderStatusEnum? clinicalOrderStatus = null,
        IEnumerable<OrderOccurrenceModel>? occurrences = null,
        IEnumerable<OrderHistoryEntryModel>? history = null)
        => new(
            ClinicalOrderId, PatientId, RegId, OrderingClinicianId, orderType ?? OrderType,
            orderSpecification ?? OrderSpecification, clinicalOrderStatus ?? ClinicalOrderStatus,
            occurrences ?? _listOccurrence, history ?? _listHistory);

    private IEnumerable<OrderHistoryEntryModel> AppendHistory(
        OrderHistoryActionEnum action,
        string actorId,
        DateTime occurredAt,
        string? reason,
        string? beforeValue,
        string? afterValue)
    {
        var latest = _listHistory.LastOrDefault();
        if (latest is not null && occurredAt < latest.OccurredAt)
            throw new InvalidOperationException("Order History harus dicatat secara kronologis.");

        return _listHistory.Append(OrderHistoryEntryModel.Create(
            action, actorId, occurredAt, reason, beforeValue, afterValue));
    }

    private IEnumerable<OrderOccurrenceModel> ReplaceOccurrence(OrderOccurrenceModel changed) =>
        _listOccurrence.Select(x => x.OrderOccurrenceId == changed.OrderOccurrenceId ? changed : x);

    private OrderOccurrenceModel FindOccurrence(string orderOccurrenceId) =>
        _listOccurrence.SingleOrDefault(x => x.OrderOccurrenceId == orderOccurrenceId)
        ?? throw new KeyNotFoundException($"Order occurrence {orderOccurrenceId} tidak ditemukan.");

    private void EnsureActive()
    {
        if (ClinicalOrderStatus != ClinicalOrderStatusEnum.Active)
            throw new InvalidOperationException(
                $"Clinical order {ClinicalOrderId} berstatus {ClinicalOrderStatus} dan tidak dapat diubah.");
    }

    private void EnsureEveryOccurrenceIsActive()
    {
        EnsureActive();
        if (_listOccurrence.Any(x => x.OrderOccurrenceStatus != OrderOccurrenceStatusEnum.Active))
            throw new InvalidOperationException("Intent dan occurrence plan hanya dapat diubah saat semua occurrence masih Active.");
    }

    private static void EnsureReason(string reason) => Guard.Against.NullOrWhiteSpace(reason);

    private static ClinicalOrderStatusEnum DeriveFulfilmentStatus(IEnumerable<OrderOccurrenceModel> occurrences)
    {
        var list = occurrences.ToList();
        if (list.Any(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Active))
            return ClinicalOrderStatusEnum.Active;
        return list.Any(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Completed)
            ? ClinicalOrderStatusEnum.Completed
            : ClinicalOrderStatusEnum.NotPerformed;
    }

    private static string DescribeIntent(OrderTypeType orderType, OrderSpecificationType specification) =>
        $"{orderType.OrderTypeId}:{orderType.OrderTypeName}|{specification.RequestedWork}|{specification.ClinicalInstructions}";

    private static string DescribePlan(IEnumerable<OrderOccurrenceModel> occurrences) =>
        string.Join(";", occurrences.Select(x => $"{x.PlannedExecutionAt:O}@{x.Destination.DestinationId}"));

    #endregion
}
