using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabOrderFeature;

public class LabOrderModel : ILabOrderKey
{
    private const string IdPrefix = "LBO";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);
    private readonly List<LabOrderItemModel> _items;
    private readonly List<LabOrderItemComponentModel> _itemComponents;

    #region CREATION

    private LabOrderModel(
        string orderId,
        string emrOrderId,
        string orderNo,
        LabOrderSourceEnum orderSource,
        LabOrderStatusEnum labOrderStatus,
        BillingReleaseValidationStatusEnum lastBillingReleaseStatus,
        OwareStatusEnum owareStatus,
        PatientSnapshotType patient,
        string executionRegId,
        DeferredInfoType deferredInfo,
        string billingTindakanId,
        string billingLastError,
        CollectionInfoType collectionInfo,
        DateTime lastBillingReleaseCheckAt,
        string lastBillingReleaseCheckUserId,
        string lastBillingReleaseMessage,
        DateTime releasedDate,
        string releasedUserId,
        string releaseNote,
        string cancelledReason,
        DateTime cancelledDate,
        string cancelledUserId,
        string terminationReason,
        DateTime terminationDate,
        string terminationUserId,
        AuditTrailType auditTrail,
        IEnumerable<LabOrderItemModel> items,
        IEnumerable<LabOrderItemComponentModel> itemComponents)
    {
        OrderId = orderId;
        EmrOrderId = emrOrderId ?? string.Empty;
        OrderNo = orderNo;
        OrderSource = orderSource;
        LabOrderStatus = labOrderStatus;
        LastBillingReleaseStatus = lastBillingReleaseStatus;
        OwareStatus = owareStatus;
        Patient = patient;
        ExecutionRegId = executionRegId;
        DeferredInfo = deferredInfo;
        BillingTindakanId = billingTindakanId;
        BillingLastError = billingLastError;
        CollectionInfo = collectionInfo;
        LastBillingReleaseCheckAt = lastBillingReleaseCheckAt;
        LastBillingReleaseCheckUserId = lastBillingReleaseCheckUserId;
        LastBillingReleaseMessage = lastBillingReleaseMessage;
        ReleasedDate = releasedDate;
        ReleasedUserId = releasedUserId;
        ReleaseNote = releaseNote;
        CancelledReason = cancelledReason;
        CancelledDate = cancelledDate;
        CancelledUserId = cancelledUserId;
        TerminationReason = terminationReason;
        TerminationDate = terminationDate;
        TerminationUserId = terminationUserId;
        AuditTrail = auditTrail;
        _items = items.ToList();
        _itemComponents = itemComponents.ToList();
    }

    public static LabOrderModel Default => new(
        orderId: "-",
        emrOrderId: "",
        orderNo: "",
        orderSource: LabOrderSourceEnum.Emr,
        labOrderStatus: LabOrderStatusEnum.Ordered,
        lastBillingReleaseStatus: BillingReleaseValidationStatusEnum.NotChecked,
        owareStatus: OwareStatusEnum.Pending,
        patient: PatientSnapshotType.Default,
        executionRegId: "",
        deferredInfo: DeferredInfoType.Default,
        billingTindakanId: "",
        billingLastError: "",
        collectionInfo: CollectionInfoType.Default,
        lastBillingReleaseCheckAt: EmptyDate,
        lastBillingReleaseCheckUserId: "",
        lastBillingReleaseMessage: "",
        releasedDate: EmptyDate,
        releasedUserId: "",
        releaseNote: "",
        cancelledReason: "",
        cancelledDate: EmptyDate,
        cancelledUserId: "",
        terminationReason: "",
        terminationDate: EmptyDate,
        terminationUserId: "",
        auditTrail: AuditTrailType.Default,
        items: [],
        itemComponents: []);

    public static ILabOrderKey Key(string orderId) => new LabOrderModel(
        orderId,
        "",
        "",
        LabOrderSourceEnum.Emr,
        LabOrderStatusEnum.Ordered,
        BillingReleaseValidationStatusEnum.NotChecked,
        OwareStatusEnum.Pending,
        PatientSnapshotType.Default,
        "",
        DeferredInfoType.Default,
        "",
        "",
        CollectionInfoType.Default,
        EmptyDate,
        "",
        "",
        EmptyDate,
        "",
        "",
        "",
        EmptyDate,
        "",
        "",
        EmptyDate,
        "",
        AuditTrailType.Default,
        [],
        []);

    public static LabOrderModel Load(
        string orderId,
        string emrOrderId,
        string orderNo,
        LabOrderSourceEnum orderSource,
        LabOrderStatusEnum labOrderStatus,
        BillingReleaseValidationStatusEnum lastBillingReleaseStatus,
        OwareStatusEnum owareStatus,
        PatientSnapshotType patient,
        string executionRegId,
        DeferredInfoType deferredInfo,
        string billingTindakanId,
        string billingLastError,
        CollectionInfoType collectionInfo,
        DateTime lastBillingReleaseCheckAt,
        string lastBillingReleaseCheckUserId,
        string lastBillingReleaseMessage,
        DateTime releasedDate,
        string releasedUserId,
        string releaseNote,
        string cancelledReason,
        DateTime cancelledDate,
        string cancelledUserId,
        string terminationReason,
        DateTime terminationDate,
        string terminationUserId,
        AuditTrailType auditTrail,
        IEnumerable<LabOrderItemModel> items,
        IEnumerable<LabOrderItemComponentModel> itemComponents)
        => new(
            orderId,
            emrOrderId,
            orderNo,
            orderSource,
            labOrderStatus,
            lastBillingReleaseStatus,
            owareStatus,
            patient,
            executionRegId,
            deferredInfo,
            billingTindakanId,
            billingLastError,
            collectionInfo,
            lastBillingReleaseCheckAt,
            lastBillingReleaseCheckUserId,
            lastBillingReleaseMessage,
            releasedDate,
            releasedUserId,
            releaseNote,
            cancelledReason,
            cancelledDate,
            cancelledUserId,
            terminationReason,
            terminationDate,
            terminationUserId,
            auditTrail,
            items,
            itemComponents);

    public static LabOrderModel CreateFromEmr(
        string emrOrderId,
        PatientSnapshotType snapshot,
        IEnumerable<ResolvedOrderLine> lines,
        string orderNo,
        AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(emrOrderId, nameof(emrOrderId));
        Guard.Against.Null(snapshot);
        Guard.Against.NullOrWhiteSpace(snapshot.RegId, nameof(snapshot.RegId));
        Guard.Against.NullOrWhiteSpace(snapshot.PatientId, nameof(snapshot.PatientId));
        Guard.Against.NullOrWhiteSpace(snapshot.PatientName, nameof(snapshot.PatientName));
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return CreateInternal(
            emrOrderId,
            LabOrderSourceEnum.Emr,
            snapshot,
            lines,
            orderNo,
            audit);
    }

    public static LabOrderModel CreateExternal(
        string? emrOrderId,
        PatientSnapshotType snapshot,
        IEnumerable<ResolvedOrderLine> lines,
        string orderNo,
        AuditInfoType audit)
    {
        Guard.Against.Null(snapshot);
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return CreateInternal(
            emrOrderId ?? string.Empty,
            LabOrderSourceEnum.ExternalPatient,
            snapshot,
            lines,
            orderNo,
            audit);
    }

    private static LabOrderModel CreateInternal(
        string emrOrderId,
        LabOrderSourceEnum orderSource,
        PatientSnapshotType snapshot,
        IEnumerable<ResolvedOrderLine> lines,
        string orderNo,
        AuditInfoType audit)
    {
        var (itemList, componentList) = AssignLineNumbers(lines);
        var orderId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new LabOrderModel(
            orderId,
            emrOrderId,
            orderNo,
            orderSource,
            LabOrderStatusEnum.Ordered,
            BillingReleaseValidationStatusEnum.NotChecked,
            OwareStatusEnum.Pending,
            snapshot,
            executionRegId: "",
            deferredInfo: DeferredInfoType.Default,
            billingTindakanId: "",
            billingLastError: "",
            collectionInfo: CollectionInfoType.Default,
            lastBillingReleaseCheckAt: EmptyDate,
            lastBillingReleaseCheckUserId: "",
            lastBillingReleaseMessage: "",
            releasedDate: EmptyDate,
            releasedUserId: "",
            releaseNote: "",
            cancelledReason: "",
            cancelledDate: EmptyDate,
            cancelledUserId: "",
            terminationReason: "",
            terminationDate: EmptyDate,
            terminationUserId: "",
            auditTrail,
            itemList,
            componentList);
    }

    private static (List<LabOrderItemModel> Items, List<LabOrderItemComponentModel> Components) AssignLineNumbers(
        IEnumerable<ResolvedOrderLine> lines)
    {
        var source = lines?.ToList() ?? [];
        Guard.Against.NullOrEmpty(source, nameof(lines));

        var items = new List<LabOrderItemModel>();
        var components = new List<LabOrderItemComponentModel>();
        var itemNo = 1;

        foreach (var line in source)
        {
            Guard.Against.Null(line.Item);
            Guard.Against.NullOrWhiteSpace(line.Item.TestDefinitionId, nameof(line.Item.TestDefinitionId));
            Guard.Against.NullOrWhiteSpace(line.Item.LabTestName, nameof(line.Item.LabTestName));

            items.Add(line.Item with { ItemNo = itemNo });

            var componentNo = 1;
            foreach (var component in line.Components.OrderBy(x => x.SequenceNo))
            {
                components.Add(component with { ItemNo = itemNo, ComponentNo = componentNo++ });
            }

            itemNo++;
        }

        return (items, components);
    }

    public record ResolvedOrderLine(
        LabOrderItemModel Item,
        IReadOnlyList<LabOrderItemComponentModel> Components);

    #endregion

    #region BEHAVIOUR

    public void Defer(string reason, DateTime untilDate, string userId)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (LabOrderStatus != LabOrderStatusEnum.Ordered)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; defer hanya diperbolehkan dari Ordered.");

        LabOrderStatus = LabOrderStatusEnum.Deferred;
        DeferredInfo = new DeferredInfoType(reason, untilDate);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void ActivateFromDeferred(string executionRegId, string userId)
    {
        Guard.Against.NullOrWhiteSpace(executionRegId, nameof(executionRegId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (LabOrderStatus != LabOrderStatusEnum.Deferred)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; activate hanya diperbolehkan dari Deferred.");

        LabOrderStatus = LabOrderStatusEnum.Ordered;
        ExecutionRegId = executionRegId;
        DeferredInfo = DeferredInfoType.Default;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Charge(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (LabOrderStatus == LabOrderStatusEnum.Deferred)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus Deferred; charge tidak diperbolehkan saat order ditunda.");

        if (LabOrderStatus != LabOrderStatusEnum.Ordered)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; charge hanya diperbolehkan dari Ordered.");

        if (!string.IsNullOrWhiteSpace(BillingTindakanId))
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah memiliki BillingTindakanId '{BillingTindakanId}'.");
    }

    public void MarkCharged(string tindakanId, string userId)
    {
        Guard.Against.NullOrWhiteSpace(tindakanId, nameof(tindakanId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        LabOrderStatus = LabOrderStatusEnum.Charged;
        BillingTindakanId = tindakanId;
        BillingLastError = "";
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void RecordBillingError(string error, string userId)
    {
        Guard.Against.NullOrWhiteSpace(error, nameof(error));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        BillingLastError = error.Length > 200 ? error[..200] : error;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void CollectSpecimen(string userId, CollectionInfoType collectionInfo)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.Null(collectionInfo);

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; pengambilan spesimen tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Charged)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; pengambilan spesimen hanya diperbolehkan dari Charged.");

        var emptyCollected = CollectionInfoType.Default.CollectedDate;
        if (collectionInfo.CollectedDate == emptyCollected)
            throw new ArgumentException("CollectedDate wajib diisi.", nameof(collectionInfo));

        var note = string.IsNullOrEmpty(collectionInfo.CollectionNote)
            ? ""
            : collectionInfo.CollectionNote.Length > 200
                ? collectionInfo.CollectionNote[..200]
                : collectionInfo.CollectionNote;

        LabOrderStatus = LabOrderStatusEnum.Collected;
        CollectionInfo = new CollectionInfoType(collectionInfo.CollectedDate, userId, note);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void MarkRecorded(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; rekaman hasil tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Charged
            && LabOrderStatus != LabOrderStatusEnum.Collected
            && LabOrderStatus != LabOrderStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; rekaman hasil hanya diperbolehkan dari Charged, Collected, atau Recorded (pembaruan).");

        if (LabOrderStatus == LabOrderStatusEnum.Charged || LabOrderStatus == LabOrderStatusEnum.Collected)
            LabOrderStatus = LabOrderStatusEnum.Recorded;

        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void MarkVerified(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; verifikasi tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; verifikasi hanya diperbolehkan dari Recorded.");

        LabOrderStatus = LabOrderStatusEnum.Verified;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    /// <summary>
    /// After a verified result is amended, order returns to Recorded until the new result version is re-verified.
    /// </summary>
    public void ReturnToRecordedAfterResultAmendment(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; amend tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Verified && LabOrderStatus != LabOrderStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; amend hanya diperbolehkan dari Verified atau Released.");

        LabOrderStatus = LabOrderStatusEnum.Recorded;
        ClearReleaseMetadata();
        ClearBillingReleaseValidationTrace();
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void RecordLastBillingReleaseValidation(
        BillingReleaseValidationStatusEnum status,
        string message,
        string checkedByUserId)
    {
        Guard.Against.NullOrWhiteSpace(checkedByUserId, nameof(checkedByUserId));

        var msg = message ?? "";
        if (msg.Length > 200)
            msg = msg[..200];

        LastBillingReleaseStatus = status;
        LastBillingReleaseCheckAt = DateTime.Now;
        LastBillingReleaseCheckUserId = checkedByUserId;
        LastBillingReleaseMessage = msg;
        AuditTrail.Modif(checkedByUserId, DateTime.Now);
    }

    public void Release(string userId, string releaseNote)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.Null(releaseNote);

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; release tidak diperbolehkan.");

        if (LabOrderStatus == LabOrderStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah Released; release bersifat final.");

        if (LabOrderStatus != LabOrderStatusEnum.Verified)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; release hanya diperbolehkan saat Verified.");

        var note = releaseNote.Length > 200 ? releaseNote[..200] : releaseNote;
        LabOrderStatus = LabOrderStatusEnum.Released;
        ReleasedDate = DateTime.Now;
        ReleasedUserId = userId;
        ReleaseNote = note;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    private void ClearReleaseMetadata()
    {
        ReleasedDate = EmptyDate;
        ReleasedUserId = "";
        ReleaseNote = "";
    }

    private void ClearBillingReleaseValidationTrace()
    {
        LastBillingReleaseStatus = BillingReleaseValidationStatusEnum.NotChecked;
        LastBillingReleaseCheckAt = EmptyDate;
        LastBillingReleaseCheckUserId = "";
        LastBillingReleaseMessage = "";
    }

    public void Cancel(string userId, string reason)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; pembatalan tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Ordered
            && LabOrderStatus != LabOrderStatusEnum.Deferred
            && LabOrderStatus != LabOrderStatusEnum.Charged)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; pembatalan hanya diperbolehkan dari Ordered, Deferred, atau Charged.");

        var r = reason.Length > 200 ? reason[..200] : reason;
        LabOrderStatus = LabOrderStatusEnum.Cancelled;
        CancelledReason = r;
        CancelledDate = DateTime.Now;
        CancelledUserId = userId;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Terminate(string userId, string reason)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; terminasi tidak diperbolehkan.");

        if (LabOrderStatus != LabOrderStatusEnum.Collected
            && LabOrderStatus != LabOrderStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; terminasi hanya diperbolehkan dari Collected atau Recorded.");

        var r = reason.Length > 200 ? reason[..200] : reason;
        LabOrderStatus = LabOrderStatusEnum.Terminated;
        TerminationReason = r;
        TerminationDate = DateTime.Now;
        TerminationUserId = userId;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void EnsureCanEnqueueOware()
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} sudah void; enqueue OWARE tidak diperbolehkan.");

        if (LabOrderStatus is LabOrderStatusEnum.Ordered
            or LabOrderStatusEnum.Deferred
            or LabOrderStatusEnum.Cancelled
            or LabOrderStatusEnum.Terminated)
            throw new InvalidOperationException(
                $"LabOrder {OrderId} berstatus {LabOrderStatus}; enqueue OWARE hanya diperbolehkan setelah Charged.");
    }

    public void MarkOwarePending(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        OwareStatus = OwareStatusEnum.Pending;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void MarkOwareSent(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        OwareStatus = OwareStatusEnum.Sent;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void MarkOwareFailed(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        OwareStatus = OwareStatusEnum.Failed;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    #endregion

    #region PROPERTIES

    public string OrderId { get; init; }
    public string EmrOrderId { get; init; }
    public string OrderNo { get; init; }
    public LabOrderSourceEnum OrderSource { get; init; }
    public LabOrderStatusEnum LabOrderStatus { get; set; }
    public BillingReleaseValidationStatusEnum LastBillingReleaseStatus { get; set; }
    public OwareStatusEnum OwareStatus { get; set; }
    public PatientSnapshotType Patient { get; init; }
    public string ExecutionRegId { get; set; }
    public DeferredInfoType DeferredInfo { get; set; }
    public string BillingTindakanId { get; set; }
    public string BillingLastError { get; set; }
    public CollectionInfoType CollectionInfo { get; set; }
    public DateTime LastBillingReleaseCheckAt { get; set; }
    public string LastBillingReleaseCheckUserId { get; set; }
    public string LastBillingReleaseMessage { get; set; }
    public DateTime ReleasedDate { get; set; }
    public string ReleasedUserId { get; set; }
    public string ReleaseNote { get; set; }
    public string CancelledReason { get; set; }
    public DateTime CancelledDate { get; set; }
    public string CancelledUserId { get; set; }
    public string TerminationReason { get; set; }
    public DateTime TerminationDate { get; set; }
    public string TerminationUserId { get; set; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<LabOrderItemModel> Items => _items;
    public IReadOnlyList<LabOrderItemComponentModel> ItemComponents => _itemComponents;

    #endregion
}
