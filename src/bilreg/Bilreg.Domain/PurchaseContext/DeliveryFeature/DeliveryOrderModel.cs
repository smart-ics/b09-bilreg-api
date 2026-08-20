using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// Lightweight supplier reference with name snapshot for historical consistency.
/// </summary>
public record SupplierReff(string SupplierId, string SupplierName)
{
    public static SupplierReff Default => new("-", "-");
}

/// <summary>
/// Delivery Order (DO) aggregate root — the originating document for purchased-goods receipt.
/// Consumed by Stock Ledger as the Goods Receipt Source (BrgMasukReffId).
/// </summary>
public class DeliveryOrderModel : IDeliveryOrderKey
{
    private const string IdPrefix = "DLV";
    private const int EmptyDateYear = 3000;

    private readonly List<DeliveryOrderItemModel> _listItem;

    #region CREATION

    public DeliveryOrderModel(
        string deliveryOrderId,
        string doNo,
        SupplierReff supplier,
        string poReffId,
        DateTime doDate,
        DeliveryOrderStateEnum state,
        string notes,
        AuditTrailType auditTrail,
        IEnumerable<DeliveryOrderItemModel> listItem)
    {
        DeliveryOrderId = deliveryOrderId;
        DoNo = doNo;
        Supplier = supplier;
        PoReffId = poReffId;
        DoDate = doDate;
        State = state;
        Notes = notes;
        AuditTrail = auditTrail;
        _listItem = listItem?.ToList() ?? [];
    }

    public static DeliveryOrderModel Default
        => new("-", "-", SupplierReff.Default, string.Empty, new DateTime(EmptyDateYear, 1, 1),
            DeliveryOrderStateEnum.Draft, "-", AuditTrailType.Default, []);

    public static IDeliveryOrderKey Key(string id)
        => new DeliveryOrderModel(id, "-", SupplierReff.Default, string.Empty, new DateTime(EmptyDateYear, 1, 1),
            DeliveryOrderStateEnum.Draft, "-", AuditTrailType.Default, []);

    public static DeliveryOrderModel Create(
        string doNo,
        SupplierReff supplier,
        string? poReffId,
        DateTime doDate,
        string notes,
        string userId,
        IEnumerable<DeliveryOrderItemModel> listItem)
    {
        Guard.Against.NullOrWhiteSpace(doNo, nameof(doNo));
        Guard.Against.Null(supplier, nameof(supplier));
        Guard.Against.Null(listItem, nameof(listItem));

        var id = NunaId.New(IdPrefix);
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);

        return new DeliveryOrderModel(
            id, doNo, supplier, poReffId ?? string.Empty, doDate,
            DeliveryOrderStateEnum.Draft, notes, auditTrail, listItem);
    }

    #endregion

    #region PROPERTIES

    public string DeliveryOrderId { get; init; }
    public string DoNo { get; private set; }
    public SupplierReff Supplier { get; private set; }
    public string PoReffId { get; private set; }
    public DateTime DoDate { get; private set; }
    public DeliveryOrderStateEnum State { get; private set; }
    public string Notes { get; private set; }
    public AuditTrailType AuditTrail { get; init; }

    public IReadOnlyCollection<DeliveryOrderItemModel> ListItem => _listItem;

    public bool IsFullyReceived =>
        _listItem.Count > 0 && _listItem.All(x => x.State == DeliveryOrderItemStateEnum.Received);

    #endregion

    #region BEHAVIOR

    public DeliveryOrderItemModel AddItem(DeliveryOrderItemModel item)
    {
        GuardDraft();
        Guard.Against.Null(item, nameof(item));
        if (item.QtyOrder <= 0)
            throw new ArgumentException("QtyOrder harus lebih dari 0", nameof(item));

        var existing = _listItem.FirstOrDefault(x =>
            x.BrgId == item.BrgId && x.LayananId == item.LayananId);
        if (existing is not null)
        {
            existing.SetQtyOrder(existing.QtyOrder + item.QtyOrder);
            return existing;
        }

        var itemNo = _listItem.Count > 0 ? _listItem.Max(x => x.ItemNo) + 1 : 1;
        item.SetItemNo(itemNo);
        _listItem.Add(item);
        return item;
    }

    public void RemoveItem(int itemNo)
    {
        GuardDraft();
        var removed = _listItem.RemoveAll(x => x.ItemNo == itemNo);
        if (removed == 0)
            throw new ArgumentException("Item tidak ditemukan", nameof(itemNo));
        Reorder();
    }

    public void SetSupplier(SupplierReff supplier)
    {
        GuardDraft();
        Guard.Against.Null(supplier, nameof(supplier));
        Supplier = supplier;
    }

    public void SetPoReff(string? poReffId)
    {
        GuardDraft();
        PoReffId = poReffId ?? string.Empty;
    }

    public void UpdateNote(string notes)
    {
        GuardDraft();
        Notes = notes;
    }

    /// <summary>
    /// Records one partial or final receipt installment against a line.
    /// The Stock Ledger consequence posting is owned by the application layer (UC-STL-001).
    /// </summary>
    public DeliveryOrderItemModel ReceiveItem(int itemNo, decimal qtyReceived, string userId, DateTime tglMutasi)
    {
        GuardStatus(DeliveryOrderStateEnum.Draft, DeliveryOrderStateEnum.Open);
        if (qtyReceived <= 0)
            throw new ArgumentException("QtyReceived harus lebih dari 0", nameof(qtyReceived));

        var item = _listItem.FirstOrDefault(x => x.ItemNo == itemNo)
            ?? throw new ArgumentException("Item tidak ditemukan", nameof(itemNo));

        item.Receive(qtyReceived);
        State = IsFullyReceived ? DeliveryOrderStateEnum.Received : DeliveryOrderStateEnum.Open;
        AuditTrail.Modif(userId, tglMutasi);
        return item;
    }

    public void Void(string userId, DateTime tglVoid)
    {
        GuardStatus(DeliveryOrderStateEnum.Draft, DeliveryOrderStateEnum.Open, DeliveryOrderStateEnum.Received);
        State = DeliveryOrderStateEnum.Void;
        AuditTrail.Batal(userId, tglVoid);
    }

    #endregion

    #region GUARD

    private void GuardDraft()
    {
        GuardStatus(DeliveryOrderStateEnum.Draft);
    }

    private void GuardStatus(params DeliveryOrderStateEnum[] allowed)
    {
        if (!allowed.Contains(State))
            throw new InvalidOperationException($"Status invalid: {State}");
    }

    private void Reorder()
    {
        var i = 1;
        foreach (var item in _listItem)
            item.SetItemNo(i++);
    }

    #endregion
}

public interface IDeliveryOrderKey
{
    string DeliveryOrderId { get; }
}