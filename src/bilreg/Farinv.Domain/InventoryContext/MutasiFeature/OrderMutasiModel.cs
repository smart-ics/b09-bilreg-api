using Ardalis.GuardClauses;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.InventoryContext.MutasiFeature;

public class OrderMutasiModel : IOrderMutasiKey
{
    private readonly List<OrderMutasiItemModel> _listItem;

    #region CREATION
    public OrderMutasiModel(string orderMutasiId, DateTime orderMutasiDate, OrderMutasiStateEnum state,
        LayananReff layananOrder, LayananReff layananTujuan, 
        ApprovalType approval, ApprovalType rejection, string orderNote, 
        AuditTrailType auditTrail, IEnumerable<OrderMutasiItemModel> listItem)
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

        _listItem = listItem?.ToList() ?? [];
    }

    public static OrderMutasiModel Default
        => new("-", new DateTime(3000, 1, 1), OrderMutasiStateEnum.Draft,
            LayananType.Default.ToReff(), LayananType.Default.ToReff(), 
            ApprovalType.Default, ApprovalType.Default,"-", AuditTrailType.Default, []);

    public static IOrderMutasiKey Key(string id)
        => new OrderMutasiModel(id, new DateTime(3000, 1, 1), OrderMutasiStateEnum.Draft,
            LayananType.Default.ToReff(), LayananType.Default.ToReff(),
            ApprovalType.Default, ApprovalType.Default, "-", AuditTrailType.Default, []);

    public static OrderMutasiModel Create(
        LayananType layananOrder,
        LayananType layananTujuan,
        string orderNote,
        string userId,
        IEnumerable<OrderMutasiItemModel> listItem)
    {
        Guard.Against.Null(layananOrder, nameof(layananOrder));
        Guard.Against.Null(layananTujuan, nameof(layananTujuan));
        Guard.Against.Null(listItem, nameof(listItem));

        var id = Ulid.NewUlid().ToString();
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);

        var result = new OrderMutasiModel(id, auditTrail.Created.Timestamp, OrderMutasiStateEnum.Draft,
            layananOrder.ToReff(), layananTujuan.ToReff(), 
            ApprovalType.Default, ApprovalType.Default, orderNote, auditTrail, listItem);
        return result;
    }
    #endregion

    #region PROPERTIES
    public string OrderMutasiId { get; init; }
    public DateTime OrderMutasiDate { get; private set; }
    public OrderMutasiStateEnum State { get; private set; }

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
            throw new ArgumentException($"Qty invalid");

        var existing = _listItem.FirstOrDefault(x => x.Brg.BrgId == item.Brg.BrgId);
        if (existing is not null)
        {
            existing.AddQty(item.Qty);
            return;
        }

        var noUrut = _listItem.Count > 0 ? _listItem.Max(x => x.NoUrut) + 1 : 1;
        item.NoUrut = noUrut;
        _listItem.Add(item);
    }

    public void Submit(LayananReff layananTujuan, string userId)
    {
        GuardDraft();
        GuardHasItem();

        State = OrderMutasiStateEnum.Submitted;
        LayananTujuan = layananTujuan;
        OrderMutasiDate = DateTime.Now;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Approve(string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Submitted);
        State = OrderMutasiStateEnum.Approved;
        Approval = new ApprovalType(userId, DateTime.Now);
    }

    public void Reject(string note, string userId)
    {
        GuardStatus(OrderMutasiStateEnum.Submitted);
        State = OrderMutasiStateEnum.Rejected;
        Rejection = new ApprovalType(userId, DateTime.Now);
        UpdateNote(note);
    }

    public void Complete()
    {
        GuardStatus(OrderMutasiStateEnum.Approved);
        State = OrderMutasiStateEnum.Completed;
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

    public void RemoveItem(IBrgKey key)
    {
        GuardDraft();
        _listItem.RemoveAll(x => x.Brg.BrgId == key.BrgId);
        Reorder();
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
    LayananReff LayananOrder, LayananReff LayananTujuan) : IOrderMutasiKey;