using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

//  M03-F01 P1-S03 — follow-up order aggregate (TD-03).
//  Lives in Charge/Tindakan context, bound to the outpatient visit by
//  identifier (RegId). Carries no reference to a queue entry or to an
//  in-progress examination, so orders remain producible after completion.
//  Lifecycle: Proposed -> Sent -> Received; cancellation only before
//  reception (from Proposed or Sent). Column-aligned with trs_tindakan_lanjut.
public record TindakanLanjutModel : ITindakanLanjutKey
{
    private readonly List<TindakanLanjutItemModel> _listItem;
    private const string ID_PREFIX = "TDL";

    #region CREATION
    public TindakanLanjutModel(
        string tindakanLanjutId, DateTime tindakanLanjutDate,
        RegReff reg, string orderType, LayananReff layanan,
        TindakanLanjutStateEnum orderState,
        DateTime sentDate, DateTime receivedDate, string receivedBy,
        string cancelReason, int rowVersion,
        IEnumerable<TindakanLanjutItemModel> listItem,
        AuditTrailType auditTrail)
    {
        TindakanLanjutId = tindakanLanjutId;
        TindakanLanjutDate = tindakanLanjutDate;
        Reg = reg;
        OrderType = orderType;
        Layanan = layanan;
        OrderState = orderState;
        SentDate = sentDate;
        ReceivedDate = receivedDate;
        ReceivedBy = receivedBy;
        CancelReason = cancelReason;
        RowVersion = rowVersion;
        _listItem = listItem.ToList() ?? [];
        AuditTrail = auditTrail;
    }

    //  New orders are always created as Proposed. The accountable receiving
    //  service arrives already resolved from mapping data (TujuanLanjutService),
    //  never from branching logic on the order type.
    public static TindakanLanjutModel Create(RegModel reg,
        TujuanLanjutType tujuanLanjut,
        IEnumerable<TindakanLanjutItemModel> listItem,
        string userId, DateTime orderedAt = default)
    {
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(tujuanLanjut, nameof(tujuanLanjut));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        if (string.IsNullOrWhiteSpace(reg.RegId))
            throw new ArgumentException("Order harus terikat pada visit (RegId)");
        if (!tujuanLanjut.IsActive)
            throw new ArgumentException($"Tipe order '{tujuanLanjut.OrderType}' tidak aktif");

        var newId = NunaId.New(ID_PREFIX);
        var occurredAt = orderedAt == default ? DateTime.Now : orderedAt;

        return new TindakanLanjutModel(newId, occurredAt,
            reg.ToReff(), tujuanLanjut.OrderType, tujuanLanjut.Layanan,
            TindakanLanjutStateEnum.Proposed,
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), string.Empty,
            string.Empty, 0,
            listItem ?? [],
            AuditTrailType.Create(userId, occurredAt));
    }

    public static TindakanLanjutModel Default => new(
        "-",
        new DateTime(3000, 1, 1),
        RegModel.Default.ToReff(), "-", LayananType.Default.ToReff(),
        TindakanLanjutStateEnum.Proposed,
        new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), string.Empty,
        string.Empty, 0,
        [],
        AuditTrailType.Default
    );

    public static ITindakanLanjutKey Key(string id) => Default with { TindakanLanjutId = id };
    #endregion

    #region PROPERTIES
    public string TindakanLanjutId { get; init; }
    public DateTime TindakanLanjutDate { get; init; }
    public RegReff Reg { get; init; }
    public string OrderType { get; init; }
    public LayananReff Layanan { get; init; }
    public TindakanLanjutStateEnum OrderState { get; private set; }
    public DateTime SentDate { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public string ReceivedBy { get; private set; }
    public string CancelReason { get; private set; }
    public int RowVersion { get; private set; }
    public IEnumerable<TindakanLanjutItemModel> ListItem => _listItem;
    public bool IsOutstanding => OrderState is TindakanLanjutStateEnum.Proposed
        or TindakanLanjutStateEnum.Sent;
    public AuditTrailType AuditTrail { get; init; }
    #endregion

    #region BEHAVIOR
    public void Send(string userId, DateTime sentAt = default)
    {
        if (OrderState != TindakanLanjutStateEnum.Proposed)
            throw new InvalidOperationException(
                $"Order {TindakanLanjutId} hanya dapat dikirim dari status Proposed");
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var occurredAt = sentAt == default ? DateTime.Now : sentAt;
        AuditTrail.Modif(userId, occurredAt);
        SentDate = occurredAt;
        OrderState = TindakanLanjutStateEnum.Sent;
        RowVersion++;
    }

    //  Idempotent: a repeated confirmation for an already received order
    //  performs no second transition and returns false.
    public bool ConfirmReceived(string receivedBy, string userId, DateTime receivedAt = default)
    {
        if (OrderState == TindakanLanjutStateEnum.Received)
            return false;
        if (OrderState != TindakanLanjutStateEnum.Sent)
            throw new InvalidOperationException(
                $"Order {TindakanLanjutId} hanya dapat dikonfirmasi dari status Sent");
        Guard.Against.NullOrWhiteSpace(receivedBy, nameof(receivedBy));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var occurredAt = receivedAt == default ? DateTime.Now : receivedAt;
        AuditTrail.Modif(userId, occurredAt);
        ReceivedDate = occurredAt;
        ReceivedBy = receivedBy;
        OrderState = TindakanLanjutStateEnum.Received;
        RowVersion++;
        return true;
    }

    public void Cancel(string cancelReason, string userId, DateTime cancelledAt = default)
    {
        if (OrderState is TindakanLanjutStateEnum.Received)
            throw new InvalidOperationException(
                $"Order {TindakanLanjutId} sudah diterima dan tidak dapat dibatalkan");
        if (OrderState is TindakanLanjutStateEnum.Cancelled)
            throw new InvalidOperationException(
                $"Order {TindakanLanjutId} sudah dibatalkan");
        Guard.Against.NullOrWhiteSpace(cancelReason, nameof(cancelReason));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var occurredAt = cancelledAt == default ? DateTime.Now : cancelledAt;
        AuditTrail.Batal(userId, occurredAt);
        CancelReason = cancelReason;
        OrderState = TindakanLanjutStateEnum.Cancelled;
        RowVersion++;
    }
    #endregion
}

public interface ITindakanLanjutKey
{
    string TindakanLanjutId { get; }
}

//  Detail lines of a follow-up order (trs_tindakan_lanjut_item).
//  Composite identity (TindakanLanjutId, ItemNo) is enforced in persistence.
public record TindakanLanjutItemModel(
    int ItemNo, string ItemCode, string ItemName, decimal Qty, string Note)
{
    public static TindakanLanjutItemModel Create(
        int itemNo, string itemCode, string itemName, decimal qty, string note = "")
    {
        if (itemNo < 1)
            throw new ArgumentException("ItemNo harus >= 1");
        Guard.Against.NullOrWhiteSpace(itemCode, nameof(itemCode));
        Guard.Against.NullOrWhiteSpace(itemName, nameof(itemName));
        if (qty <= 0)
            throw new ArgumentException("Qty harus > 0");

        return new TindakanLanjutItemModel(itemNo, itemCode, itemName, qty, note ?? string.Empty);
    }
}

//  Mirrors CK_trs_tindakan_lanjut_OrderState: 0=Proposed, 1=Sent,
//  2=Received, 3=Cancelled.
public enum TindakanLanjutStateEnum
{
    Proposed,
    Sent,
    Received,
    Cancelled
}
