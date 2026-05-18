using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabOrderFeature;

public class LabOrderModel : ILabOrderKey
{
    private const string IdPrefix = "LBO";
    private readonly List<LabOrderItemModel> _items;

    #region CREATION

    private LabOrderModel(
        string orderId,
        string orderNo,
        LabOrderSourceEnum orderSource,
        LabOrderStatusEnum labOrderStatus,
        FinancialClearanceEnum financialClearance,
        OwareStatusEnum owareStatus,
        PatientSnapshotType patient,
        string executionRegId,
        DeferredInfoType deferredInfo,
        string billingTindakanId,
        string billingLastError,
        AuditTrailType auditTrail,
        IEnumerable<LabOrderItemModel> items)
    {
        OrderId = orderId;
        OrderNo = orderNo;
        OrderSource = orderSource;
        LabOrderStatus = labOrderStatus;
        FinancialClearance = financialClearance;
        OwareStatus = owareStatus;
        Patient = patient;
        ExecutionRegId = executionRegId;
        DeferredInfo = deferredInfo;
        BillingTindakanId = billingTindakanId;
        BillingLastError = billingLastError;
        AuditTrail = auditTrail;
        _items = items.ToList();
    }

    public static LabOrderModel Default => new(
        orderId: "-",
        orderNo: "",
        orderSource: LabOrderSourceEnum.Emr,
        labOrderStatus: LabOrderStatusEnum.Ordered,
        financialClearance: FinancialClearanceEnum.Pending,
        owareStatus: OwareStatusEnum.Pending,
        patient: PatientSnapshotType.Default,
        executionRegId: "",
        deferredInfo: DeferredInfoType.Default,
        billingTindakanId: "",
        billingLastError: "",
        auditTrail: AuditTrailType.Default,
        items: []);

    public static ILabOrderKey Key(string orderId) => new LabOrderModel(
        orderId,
        "",
        LabOrderSourceEnum.Emr,
        LabOrderStatusEnum.Ordered,
        FinancialClearanceEnum.Pending,
        OwareStatusEnum.Pending,
        PatientSnapshotType.Default,
        "",
        DeferredInfoType.Default,
        "",
        "",
        AuditTrailType.Default,
        []);

    public static LabOrderModel Load(
        string orderId,
        string orderNo,
        LabOrderSourceEnum orderSource,
        LabOrderStatusEnum labOrderStatus,
        FinancialClearanceEnum financialClearance,
        OwareStatusEnum owareStatus,
        PatientSnapshotType patient,
        string executionRegId,
        DeferredInfoType deferredInfo,
        string billingTindakanId,
        string billingLastError,
        AuditTrailType auditTrail,
        IEnumerable<LabOrderItemModel> items)
        => new(
            orderId,
            orderNo,
            orderSource,
            labOrderStatus,
            financialClearance,
            owareStatus,
            patient,
            executionRegId,
            deferredInfo,
            billingTindakanId,
            billingLastError,
            auditTrail,
            items);

    public static LabOrderModel CreateFromEmr(
        PatientSnapshotType snapshot,
        IEnumerable<LabOrderItemModel> items,
        string orderNo,
        AuditInfoType audit)
    {
        Guard.Against.Null(snapshot);
        Guard.Against.NullOrWhiteSpace(snapshot.RegId, nameof(snapshot.RegId));
        Guard.Against.NullOrWhiteSpace(snapshot.PatientId, nameof(snapshot.PatientId));
        Guard.Against.NullOrWhiteSpace(snapshot.PatientName, nameof(snapshot.PatientName));
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return CreateInternal(
            LabOrderSourceEnum.Emr,
            snapshot,
            items,
            orderNo,
            audit);
    }

    public static LabOrderModel CreateExternal(
        PatientSnapshotType snapshot,
        IEnumerable<LabOrderItemModel> items,
        string orderNo,
        AuditInfoType audit)
    {
        Guard.Against.Null(snapshot);
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return CreateInternal(
            LabOrderSourceEnum.ExternalPatient,
            snapshot,
            items,
            orderNo,
            audit);
    }

    private static LabOrderModel CreateInternal(
        LabOrderSourceEnum orderSource,
        PatientSnapshotType snapshot,
        IEnumerable<LabOrderItemModel> items,
        string orderNo,
        AuditInfoType audit)
    {
        var itemList = AssignItemNumbers(items);
        var orderId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new LabOrderModel(
            orderId,
            orderNo,
            orderSource,
            LabOrderStatusEnum.Ordered,
            FinancialClearanceEnum.Pending,
            OwareStatusEnum.Pending,
            snapshot,
            executionRegId: "",
            deferredInfo: DeferredInfoType.Default,
            billingTindakanId: "",
            billingLastError: "",
            auditTrail,
            itemList);
    }

    private static List<LabOrderItemModel> AssignItemNumbers(IEnumerable<LabOrderItemModel> items)
    {
        var source = items?.ToList() ?? [];
        Guard.Against.NullOrEmpty(source, nameof(items));

        var result = new List<LabOrderItemModel>();
        var no = 1;
        foreach (var item in source)
        {
            Guard.Against.NullOrWhiteSpace(item.TestId, nameof(item.TestId));
            Guard.Against.NullOrWhiteSpace(item.TestName, nameof(item.TestName));
            result.Add(item with { ItemNo = no++ });
        }

        return result;
    }

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

    #endregion

    #region PROPERTIES

    public string OrderId { get; init; }
    public string OrderNo { get; init; }
    public LabOrderSourceEnum OrderSource { get; init; }
    public LabOrderStatusEnum LabOrderStatus { get; set; }
    public FinancialClearanceEnum FinancialClearance { get; init; }
    public OwareStatusEnum OwareStatus { get; init; }
    public PatientSnapshotType Patient { get; init; }
    public string ExecutionRegId { get; set; }
    public DeferredInfoType DeferredInfo { get; set; }
    public string BillingTindakanId { get; set; }
    public string BillingLastError { get; set; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<LabOrderItemModel> Items => _items;

    #endregion
}
