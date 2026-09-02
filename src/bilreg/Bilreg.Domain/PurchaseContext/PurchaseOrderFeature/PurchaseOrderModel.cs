using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

public class PurchaseOrderModel : IPurchaseOrderKey
{
    private readonly List<PurchaseOrderItemType> _listItem;

    public PurchaseOrderModel(string id, bool isClosed, string keterangan, PartnerReff partner, decimal subTotal,
        decimal taxTotal, decimal total, decimal diskonLain, decimal biayaLain, decimal grandTotal, AuditInfoType opened,
        AuditInfoType closed, AuditInfoType approved, AuditInfoType printed, AuditTrailType auditTrail,
        IEnumerable<PurchaseOrderItemType> listPurchaseOrderItem)
    {
        PurchaseOrderId = id;
        IsClosed = isClosed;
        Keterangan = keterangan;
        Partner = partner;
        SubTotal = subTotal;
        TaxTotal = taxTotal;
        Total = total;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        GrandTotal = grandTotal;
        Opened = opened;
        Closed = closed;
        Approved = approved;
        Printed = printed;
        AuditTrail = auditTrail;
        _listItem = [.. listPurchaseOrderItem];
    }

    public static IPurchaseOrderKey Key(string id) => new PurchaseOrderModel(id, false, AppConst.DASH,
        PartnerType.Default.ToReff(), 0, 0, 0, 0, 0, 0, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
        AuditInfoType.Default, AuditTrailType.Default, []);

    public static PurchaseOrderModel Create(
        PartnerType partner,
        string keterangan,
        string userId,
        DateTime occuredAt)
    {
        var newId = NunaId.NewLegacy("PO", 'A');
        var auditTrail = AuditTrailType.Create(userId, occuredAt);
        var model = new PurchaseOrderModel(newId, false, keterangan, partner.ToReff(), 0, 0, 0, 0, 0, 0,
            new AuditInfoType(userId, occuredAt), AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, auditTrail,
            []);
        return model;
    }

    public string PurchaseOrderId { get; private set; }
    public bool IsClosed { get; private set; }
    public string Keterangan { get; private set; }
    public PartnerReff Partner { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal GrandTotal { get; private set; }

    public AuditInfoType Opened { get; private set; }
    public AuditInfoType Closed { get; private set; }
    public AuditInfoType Approved { get; private set; }
    public AuditInfoType Printed { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }

    public IEnumerable<PurchaseOrderItemType> ListItem => _listItem;

    private void Recalculate()
    {
        SubTotal = _listItem.Sum(item => item.Subtotal);
        TaxTotal = _listItem.Sum(item => item.TaxTotal);
        Total = _listItem.Sum(item => item.Total);
        GrandTotal = Total + BiayaLain - DiskonLain;
    }

    public void AddItem(IBrg brg, SatuanType satuan, decimal harga, decimal qty, decimal qtyStok, decimal diskonPercentage,
        decimal taxPercentage, decimal biayaLain)
    {
        if (IsClosed)
            throw new ArgumentException("Cannot add items to closed Purchase Order");

        var isBrgDuplicated = _listItem
            .Any(x => x.Brg.BrgId == brg.BrgId);

        if (isBrgDuplicated)
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{brg}");

        var noUrut = ListItem
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var newItem = PurchaseOrderItemType.Create(noUrut, brg, satuan, harga, qty, qtyStok, diskonPercentage, taxPercentage,
            biayaLain);
        _listItem.Add(newItem);
        Recalculate();
    }

    public void RemoveItem(IBrg brg)
    {
        if (IsClosed)
            throw new ArgumentException("Cannot remove items from closed Purchase Order");

        _listItem.RemoveAll(x => x.Brg.BrgId == brg.BrgId);
        var i = 1;
        foreach (var item in _listItem)
        {
            item.SetNoUrut(i);
            i++;
        }

        Recalculate();
    }

    public void Open(string userId, DateTime timestamp)
    {
        if (!IsClosed)
            throw new ArgumentException("Purchase Order is not in closed status");

        IsClosed = false;
        Opened = new AuditInfoType(userId, timestamp);
    }

    public void Close(string userId, DateTime timestamp)
    {
        if (IsClosed)
            throw new ArgumentException("Purchase Order is not in opened status"); 
        
        IsClosed = true;
        Closed = new AuditInfoType(userId, timestamp);
    }

    public void Approve(string userId, DateTime timestamp)
    {
        if (!IsClosed)
            throw new ArgumentException("Purchase Order is not in opened status");

        Approved = new AuditInfoType(userId, timestamp);
    }

    public void Print(string userId, DateTime timestamp)
    {
        if (!IsClosed)
            throw new ArgumentException("Purchase Order is not in opened status");

        Printed = new AuditInfoType(userId, timestamp);
    }

    public void Void(string userId, DateTime timestamp) => AuditTrail.Batal(userId, timestamp);
}