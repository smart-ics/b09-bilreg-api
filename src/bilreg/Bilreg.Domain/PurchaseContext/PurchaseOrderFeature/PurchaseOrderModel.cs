using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

public class PurchaseOrderModel : IPurchaseOrderKey
{
    private readonly List<PurchaseOrderItemType> _listItem;

    public PurchaseOrderModel(string id, string keterangan, PartnerReff partner, decimal subTotal, decimal taxTotal,
        decimal total, decimal diskonLain, decimal biayaLain, decimal grandTotal, AuditTrailType auditTrail,
        IEnumerable<PurchaseOrderItemType> listPurchaseOrderItem)
    {
        PurchaseOrderId = id;
        Keterangan = keterangan;
        Partner = partner;
        SubTotal = subTotal;
        TaxTotal = taxTotal;
        Total = total;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        GrandTotal = grandTotal;
        AuditTrail = auditTrail;
        _listItem = [.. listPurchaseOrderItem];
    }

    public static IPurchaseOrderKey Key(string id) => new PurchaseOrderModel(id, AppConst.DASH, PartnerType.Default.ToReff(), 0,
        0, 0, 0, 0, 0, AuditTrailType.Default, []);

    public static PurchaseOrderModel Create(PartnerType partner, string keterangan, string userId)
    {
        var newId = NunaId.NewLegacy("PO", 'A');
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
        var model = new PurchaseOrderModel(newId, keterangan, partner.ToReff(), 0, 0, 0, 0, 0, 0, auditTrail, []);
        return model;
    }

    public string PurchaseOrderId { get; private set; }
    public string Keterangan { get; private set; }
    public PartnerReff Partner { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal GrandTotal { get; private set; }

    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<PurchaseOrderItemType> ListItem => _listItem;

    public void AddItem(IBrg brg, SatuanType satuan, decimal qty)
    {
        var isBrgDuplicated = _listItem
            .Any(x => x.Brg.BrgId == brg.BrgId);

        if (isBrgDuplicated)
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{brg}");

        var noUrut = ListItem
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var newItem = new PurchaseOrderItemType(noUrut, brg.ToReff(), satuan, 0, qty, 0, 0, 0, 0, 0, 0, 0, 0);
        _listItem.Add(newItem);   
    }
}