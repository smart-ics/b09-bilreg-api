using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.RadContext.RadOrderFeature;

public class RadOrderModel : IRadOrderKey
{
    public const string IdPrefix = "RDO";
    public const string OrderNoPrefix = "RAD";
    public static readonly DateTime EmptyDate = new(3000, 1, 1);

    private readonly List<RadOrderItemModel> _items;

    #region CREATION

    public RadOrderModel(
        string orderId,
        string emrOrderId,
        string orderNo,
        RadOrderSourceEnum orderSource,
        RadModalityEnum modality,
        RadOrderStatusEnum radOrderStatus,
        RadUrgencyEnum urgency,
        PatientSnapshotType patient,
        string executionRegId,
        string dokterPengorderId,
        string dokterPengorderName,
        string layananPengorderId,
        string layananPengorderName,
        string clinicalNote,
        string cancelledReason,
        DateTime cancelledDate,
        string cancelledUserId,
        AuditTrailType auditTrail,
        IEnumerable<RadOrderItemModel>? items = null)
    {
        OrderId = orderId ?? string.Empty;
        EmrOrderId = emrOrderId ?? string.Empty;
        OrderNo = orderNo ?? string.Empty;
        OrderSource = orderSource;
        Modality = modality;
        RadOrderStatus = radOrderStatus;
        Urgency = urgency;
        Patient = patient ?? PatientSnapshotType.Default;
        ExecutionRegId = executionRegId ?? string.Empty;
        DokterPengorderId = dokterPengorderId ?? string.Empty;
        DokterPengorderName = dokterPengorderName ?? string.Empty;
        LayananPengorderId = layananPengorderId ?? string.Empty;
        LayananPengorderName = layananPengorderName ?? string.Empty;
        ClinicalNote = clinicalNote ?? string.Empty;
        CancelledReason = cancelledReason ?? string.Empty;
        CancelledDate = cancelledDate == default ? EmptyDate : cancelledDate;
        CancelledUserId = cancelledUserId ?? string.Empty;
        AuditTrail = auditTrail ?? AuditTrailType.Default;

        _items = new List<RadOrderItemModel>();
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.Modality != Modality)
                    throw new ArgumentException(
                        $"Item {item.TarifId} memiliki modalitas {item.Modality} yang berbeda dari modalitas order {Modality}.",
                        nameof(items));

                var nextItemNo = _items.Count + 1;
                var itemToAdd = item with { OrderId = OrderId, ItemNo = item.ItemNo > 0 ? item.ItemNo : nextItemNo };
                _items.Add(itemToAdd);
            }
        }
    }

    public static RadOrderModel Default => new(
        orderId: "-",
        emrOrderId: string.Empty,
        orderNo: string.Empty,
        orderSource: RadOrderSourceEnum.Internal,
        modality: RadModalityEnum.CR,
        radOrderStatus: RadOrderStatusEnum.Requested,
        urgency: RadUrgencyEnum.Rutin,
        patient: PatientSnapshotType.Default,
        executionRegId: string.Empty,
        dokterPengorderId: string.Empty,
        dokterPengorderName: string.Empty,
        layananPengorderId: string.Empty,
        layananPengorderName: string.Empty,
        clinicalNote: string.Empty,
        cancelledReason: string.Empty,
        cancelledDate: EmptyDate,
        cancelledUserId: string.Empty,
        auditTrail: AuditTrailType.Default);

    public static RadOrderModel CreateFromEmr(
        string emrOrderId,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        IEnumerable<RadOrderItemModel> items,
        string orderNo,
        AuditInfoType audit,
        string dokterPengorderId = "",
        string dokterPengorderName = "",
        string layananPengorderId = "",
        string layananPengorderName = "",
        string clinicalNote = "",
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
    {
        Guard.Against.NullOrWhiteSpace(emrOrderId, nameof(emrOrderId));
        Guard.Against.Null(patient, nameof(patient));
        Guard.Against.NullOrWhiteSpace(patient.RegId, nameof(patient.RegId));
        Guard.Against.NullOrWhiteSpace(patient.PatientId, nameof(patient.PatientId));
        Guard.Against.NullOrWhiteSpace(patient.PatientName, nameof(patient.PatientName));
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit, nameof(audit));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var itemList = items?.ToList() ?? new List<RadOrderItemModel>();
        foreach (var item in itemList)
        {
            if (item.Modality != modality)
                throw new ArgumentException(
                    $"Item {item.TarifId} memiliki modalitas {item.Modality} yang berbeda dari modalitas order {modality}.",
                    nameof(items));
        }

        var orderId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new RadOrderModel(
            orderId,
            emrOrderId,
            orderNo,
            RadOrderSourceEnum.Internal,
            modality,
            RadOrderStatusEnum.Requested,
            urgency,
            patient,
            patient.RegId,
            dokterPengorderId,
            dokterPengorderName,
            layananPengorderId,
            layananPengorderName,
            clinicalNote,
            string.Empty,
            EmptyDate,
            string.Empty,
            auditTrail,
            itemList);
    }

    public static RadOrderModel CreateFromEmr(
        string emrOrderId,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        string orderNo,
        AuditInfoType audit,
        string dokterPengorderId = "",
        string dokterPengorderName = "",
        string layananPengorderId = "",
        string layananPengorderName = "",
        string clinicalNote = "",
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => CreateFromEmr(
            emrOrderId,
            patient,
            modality,
            Enumerable.Empty<RadOrderItemModel>(),
            orderNo,
            audit,
            dokterPengorderId,
            dokterPengorderName,
            layananPengorderId,
            layananPengorderName,
            clinicalNote,
            urgency);

    public static RadOrderModel CreateExternal(
        string? emrOrderId,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        IEnumerable<RadOrderItemModel> items,
        string orderNo,
        AuditInfoType audit,
        string dokterPengorderId = "",
        string dokterPengorderName = "",
        string layananPengorderId = "",
        string layananPengorderName = "",
        string clinicalNote = "",
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
    {
        Guard.Against.Null(patient, nameof(patient));
        Guard.Against.NullOrWhiteSpace(patient.PatientName, nameof(patient.PatientName));
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(audit, nameof(audit));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var itemList = items?.ToList() ?? new List<RadOrderItemModel>();
        foreach (var item in itemList)
        {
            if (item.Modality != modality)
                throw new ArgumentException(
                    $"Item {item.TarifId} memiliki modalitas {item.Modality} yang berbeda dari modalitas order {modality}.",
                    nameof(items));
        }

        var orderId = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new RadOrderModel(
            orderId,
            emrOrderId ?? string.Empty,
            orderNo,
            RadOrderSourceEnum.External,
            modality,
            RadOrderStatusEnum.Requested,
            urgency,
            patient,
            patient.RegId ?? string.Empty,
            dokterPengorderId,
            dokterPengorderName,
            layananPengorderId,
            layananPengorderName,
            clinicalNote,
            string.Empty,
            EmptyDate,
            string.Empty,
            auditTrail,
            itemList);
    }

    public static RadOrderModel CreateExternal(
        string? emrOrderId,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        string orderNo,
        AuditInfoType audit,
        string dokterPengorderId = "",
        string dokterPengorderName = "",
        string layananPengorderId = "",
        string layananPengorderName = "",
        string clinicalNote = "",
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => CreateExternal(
            emrOrderId,
            patient,
            modality,
            Enumerable.Empty<RadOrderItemModel>(),
            orderNo,
            audit,
            dokterPengorderId,
            dokterPengorderName,
            layananPengorderId,
            layananPengorderName,
            clinicalNote,
            urgency);

    #endregion

    #region INVARIANTS & METHODS

    public void AddItem(RadOrderItemModel item)
    {
        Guard.Against.Null(item, nameof(item));

        if (item.Modality != Modality)
            throw new ArgumentException(
                $"Item {item.TarifId} memiliki modalitas {item.Modality} yang berbeda dari modalitas order {Modality}.",
                nameof(item));

        var nextItemNo = _items.Count + 1;
        var itemToAdd = item with { OrderId = OrderId, ItemNo = nextItemNo };
        _items.Add(itemToAdd);
    }

    public void Schedule(string userId, DateTime scheduledAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Order {OrderId} sudah void; penjadwalan tidak diizinkan.");

        if (RadOrderStatus != RadOrderStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Order {OrderId} berstatus {RadOrderStatus}; penjadwalan hanya diizinkan untuk status Requested.");

        var at = scheduledAt == default ? DateTime.Now : scheduledAt;
        RadOrderStatus = RadOrderStatusEnum.Scheduled;
        AuditTrail.Modif(userId, at);
    }

    public void Cancel(string reason, string userId, DateTime cancelDate = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("Alasan pembatalan minimal 3 karakter.", nameof(reason));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Order {OrderId} sudah void; pembatalan tidak diizinkan.");

        if (RadOrderStatus != RadOrderStatusEnum.Requested && RadOrderStatus != RadOrderStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Order {OrderId} berstatus {RadOrderStatus}; pembatalan hanya diizinkan untuk status Requested atau Scheduled.");

        var at = cancelDate == default ? DateTime.Now : cancelDate;
        var r = reason.Trim();
        if (r.Length > 200)
            r = r[..200];

        RadOrderStatus = RadOrderStatusEnum.Cancelled;
        CancelledReason = r;
        CancelledDate = at;
        CancelledUserId = userId.Trim();
        AuditTrail.Modif(userId, at);
    }

    #endregion

    #region PROPERTIES

    public string OrderId { get; init; }
    public string EmrOrderId { get; init; }
    public string OrderNo { get; init; }
    public RadOrderSourceEnum OrderSource { get; init; }
    public RadModalityEnum Modality { get; init; }
    public RadOrderStatusEnum RadOrderStatus { get; private set; }
    public RadUrgencyEnum Urgency { get; set; }
    public PatientSnapshotType Patient { get; init; }
    public string ExecutionRegId { get; set; }
    public string DokterPengorderId { get; init; }
    public string DokterPengorderName { get; init; }
    public string LayananPengorderId { get; init; }
    public string LayananPengorderName { get; init; }
    public string ClinicalNote { get; set; }
    public string CancelledReason { get; set; }
    public DateTime CancelledDate { get; set; }
    public string CancelledUserId { get; set; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<RadOrderItemModel> Items => _items;

    public bool IsCito => Urgency == RadUrgencyEnum.Cito || _items.Any(x => x.IsCito);
    public bool IsCancelled => RadOrderStatus == RadOrderStatusEnum.Cancelled;
    public bool IsActive => !IsCancelled && !AuditTrail.IsVoided;

    #endregion
}
