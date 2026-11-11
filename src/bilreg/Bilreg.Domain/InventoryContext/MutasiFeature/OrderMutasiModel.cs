using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class OrderMutasiModel : IOrderMutasiKey
{
    private readonly List<OrderMutasiItemModel> _listItem;

    #region CREATION
    public OrderMutasiModel(string orderMutasiId, DateTime orderMutasiDate, OrderMutasiStateEnum state,
        LayananReff layananOrder, LayananReff layananTujuan, 
        ApprovalType approval, ApprovalType rejection, string orderNote, 
        AuditTrailType auditTrail, IEnumerable<OrderMutasiItemModel> listItem,
        OrderMutasiPriorityEnum priority = OrderMutasiPriorityEnum.Rutin)
    {
        OrderMutasiId = orderMutasiId;
        OrderMutasiDate = orderMutasiDate;
        State = state;
        LayananOrder = layananOrder;
        LayananTujuan = layananTujuan;
        Approval = approval;
        Rejection = rejection;
        OrderNote = orderNote;
        AuditTrail = auditTrail;
        Priority = priority;

        _listItem = listItem?.ToList() ?? [];
    }

    public static OrderMutasiModel Default
        => new("-", new DateTime(3000, 1, 1), OrderMutasiStateEnum.Draft,
            LayananType.Default.ToReff(), LayananType.Default.ToReff(), 
            ApprovalType.Default, ApprovalType.Default, "-", AuditTrailType.Default, [],
            OrderMutasiPriorityEnum.Rutin);

    public static IOrderMutasiKey Key(string id)
        => new OrderMutasiModel(id, new DateTime(3000, 1, 1), OrderMutasiStateEnum.Draft,
            LayananType.Default.ToReff(), LayananType.Default.ToReff(),
            ApprovalType.Default, ApprovalType.Default, "-", AuditTrailType.Default, [],
            OrderMutasiPriorityEnum.Rutin);

    public static OrderMutasiModel Create(
        LayananType layananOrder,
        LayananType layananTujuan,
        string orderNote,
        string userId,
        IEnumerable<OrderMutasiItemModel> listItem,
        OrderMutasiPriorityEnum priority = OrderMutasiPriorityEnum.Rutin)
    {
        Guard.Against.Null(layananOrder, nameof(layananOrder));
        Guard.Against.Null(layananTujuan, nameof(layananTujuan));
        Guard.Against.Null(listItem, nameof(listItem));

        var id = Ulid.NewUlid().ToString();
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);

        var result = new OrderMutasiModel(id, auditTrail.Created.Timestamp, OrderMutasiStateEnum.Draft,
            layananOrder.ToReff(), layananTujuan.ToReff(), 
            ApprovalType.Default, ApprovalType.Default, orderNote, auditTrail, listItem, priority);
        return result;
    }
    #endregion

    #region PROPERTIES
    public string OrderMutasiId { get; init; }
    public DateTime OrderMutasiDate { get; private set; }
    public OrderMutasiStateEnum State { get; private set; }
    public OrderMutasiPriorityEnum Priority { get; private set; }

    public LayananReff LayananOrder { get; init; }
    public LayananReff LayananTujuan { get; private set; }

    public ApprovalType Approval { get; private set; }
    public ApprovalType Rejection { get; private set; }
    public string OrderNote { get; private set; }

    public AuditTrailType AuditTrail { get; init; }

    public IReadOnlyCollection<OrderMutasiItemModel> ListItem => _listItem;

    #endregion

    #region BEHAVIOR
    public void AddItem(OrderMutasiItemModel item)
    {
        GuardDraft();
        if (item.Qty <= 0)
            throw new ArgumentException("Qty invalid");

        var existing = _listItem.FirstOrDefault(x => x.Brg.BrgId == item.Brg.BrgId);
        if (existing is not null)
        {
            existing.AddQty(item.Qty);
            existing.SetStokCurrent(item.StokCurrent);
            return;
        }

        var noUrut = _listItem.Count > 0 ? _listItem.Max(x => x.NoUrut) + 1 : 1;
        item.NoUrut = noUrut;
        _listItem.Add(item);
    }

    public void SetItemQty(IBrgKey key, decimal qty, string userId, decimal? stokCurrent = null)
    {
        GuardStatus(OrderMutasiStateEnum.Draft, OrderMutasiStateEnum.Submitted);
        if (qty < 0)
            throw new ArgumentException("Qty tidak boleh negatif");

        if (qty == 0)
        {
            RemoveItem(key, userId);
            return;
        }

        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == key.BrgId)
            ?? throw new ArgumentException("Item tidak ditemukan");

        item.SetQty(qty);
        if (stokCurrent.HasValue)
        {
            item.SetStokCurrent(stokCurrent.Value);
        }
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void RemoveItem(IBrgKey key, string? userId = null)
    {
        GuardStatus(OrderMutasiStateEnum.Draft, OrderMutasiStateEnum.Submitted);
        _listItem.RemoveAll(x => x.Brg.BrgId == key.BrgId);
        Reorder();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            AuditTrail.Modif(userId, DateTime.Now);
        }
    }

    public void Submit(LayananReff layananTujuan, string userId, OrderMutasiPriorityEnum priority = OrderMutasiPriorityEnum.Rutin, string? orderNote = null, bool autoApprove = false)
    {
        GuardDraft();
        GuardHasItem();

        if (string.Equals(LayananOrder.LayananId, layananTujuan.LayananId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unit layanan peminta dan unit layanan tujuan tidak boleh sama");

        LayananTujuan = layananTujuan;
        Priority = priority;
        if (!string.IsNullOrWhiteSpace(orderNote))
        {
            OrderNote = orderNote;
        }
        OrderMutasiDate = DateTime.Now;
        State = OrderMutasiStateEnum.Submitted;
        AuditTrail.Modif(userId, DateTime.Now);

        if (autoApprove)
        {
            Approve(userId);
        }
    }

    public void Approve(string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Submitted);
        State = OrderMutasiStateEnum.Approved;
        Approval = new ApprovalType(userId, DateTime.Now);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Reject(string note, string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Submitted);
        State = OrderMutasiStateEnum.Rejected;
        Rejection = new ApprovalType(userId, DateTime.Now);
        UpdateNote(note);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void RecordFulfillment(IBrgKey brgKey, decimal fulfilledQty, string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Approved, OrderMutasiStateEnum.PartiallyFulfilled);
        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == brgKey.BrgId)
            ?? throw new ArgumentException("Item tidak ditemukan");

        item.AddFulfilledQty(fulfilledQty);

        if (_listItem.All(x => x.SisaBackorder == 0))
        {
            State = OrderMutasiStateEnum.Completed;
        }
        else
        {
            State = OrderMutasiStateEnum.PartiallyFulfilled;
        }

        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void ForceClose(string reason, string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Approved, OrderMutasiStateEnum.PartiallyFulfilled);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Alasan force close wajib diisi", nameof(reason));

        State = OrderMutasiStateEnum.ForceClosed;
        UpdateNote(reason);
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Cancel(string reason, string userId)
    {
        if (State == OrderMutasiStateEnum.PartiallyFulfilled || _listItem.Any(x => x.QtyTerpenuhi > 0))
        {
            throw new InvalidOperationException("Permintaan mutasi yang telah dipenuhi sebagian tidak dapat dibatalkan, gunakan ForceClose");
        }

        GuardStatus(OrderMutasiStateEnum.Draft, OrderMutasiStateEnum.Submitted, OrderMutasiStateEnum.Approved);

        State = OrderMutasiStateEnum.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            UpdateNote(reason);
        }
        AuditTrail.Batal(userId, DateTime.Now);
    }

    public void Complete()
    {
        GuardStatus(OrderMutasiStateEnum.Approved, OrderMutasiStateEnum.PartiallyFulfilled);
        State = OrderMutasiStateEnum.Completed;
    }

    public void Reopen()
    {
        GuardStatus(OrderMutasiStateEnum.Completed);
        State = OrderMutasiStateEnum.Approved;
    }

    public void UpdateNote(string note)
    {
        OrderNote = note;
    }

    public void SetLayananTujuan(LayananReff tujuan)
    {
        GuardDraft();
        LayananTujuan = tujuan;
    }

    public void MoveItem(IBrgKey key, int targetNoUrut)
    {
        GuardDraft();

        if (_listItem.Count <= 1)
            return;

        if (targetNoUrut <= 0)
            throw new ArgumentException("NoUrut tidak valid");

        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == key.BrgId)
            ?? throw new ArgumentException("Item tidak ditemukan");
        if (targetNoUrut > _listItem.Count)
            targetNoUrut = _listItem.Count;

        _listItem.Remove(item);
        _listItem.Insert(targetNoUrut - 1, item);

        Reorder();
    }

    private void Reorder()
    {
        var i = 1;
        foreach (var item in _listItem)
            item.SetNoUrut(i++);
    }
    #endregion

    #region GUARD
    private void GuardDraft()
    {
        GuardStatus(OrderMutasiStateEnum.Draft);
    }

    private void GuardHasItem()
    {
        if (_listItem.Count == 0)
            throw new ArgumentException("Order Mutasi harus memiliki minimal 1 item");
    }

    private void GuardStatus(params OrderMutasiStateEnum[] allowed)
    {
        if (!allowed.Contains(State))
            throw new ArgumentException($"Status invalid: {State}");
    }
    #endregion
}

public interface IOrderMutasiKey
{
    string OrderMutasiId { get; }
}

public record OrderMutasiHeaderView(string OrderMutasiId,
    DateTime OrderMutasiDate, OrderMutasiStateEnum State,
    LayananReff LayananOrder, LayananReff LayananTujuan,
    OrderMutasiPriorityEnum Priority = OrderMutasiPriorityEnum.Rutin,
    string OrderNote = "-") : IOrderMutasiKey;
