using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

public record PurchaseOrderItemType
{
    public PurchaseOrderItemType(int noUrut, BrgReff brg, SatuanType satuan, decimal harga, decimal qty, decimal qtyStok,
        decimal subtotal, decimal diskonPercentage, decimal diskonTotal, decimal taxPercentage, decimal taxTotal,
        decimal biayaLain, decimal total)
    {
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Harga = harga;
        Qty = qty;
        QtyStok = qtyStok;
        Subtotal = subtotal;
        DiskonPercentage = diskonPercentage;
        DiskonTotal = diskonTotal;
        TaxPercentage = taxPercentage;
        TaxTotal = taxTotal;
        BiayaLain = biayaLain;
        Total = total;
    }

    public static PurchaseOrderItemType Create(int noUrut, IBrg brg, SatuanType satuan, decimal harga, decimal qty,
        decimal qtyStok, decimal diskonPercentage, decimal taxPercentage, decimal biayaLain)
    {
        var diskonPerItem = harga * diskonPercentage / 100;
        var subtotal = harga * qty - diskonPerItem * qty;
        var taxTotal = subtotal * taxPercentage / 100;
        var total = subtotal + taxTotal + biayaLain;

        return new PurchaseOrderItemType(noUrut, brg.ToReff(), satuan, harga, qty, qtyStok, subtotal, diskonPercentage,
            diskonPerItem, taxPercentage, taxTotal, biayaLain, total);
    }

    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }

    public decimal Harga { get; init; }
    public decimal QtyStok { get; init; }
    public decimal Qty { get; init; }
    public decimal Subtotal { get; init; }

    public decimal DiskonPercentage { get; init; }
    public decimal DiskonTotal { get; init; }

    public decimal TaxPercentage { get; init; }
    public decimal TaxTotal { get; init; }

    public decimal BiayaLain { get; init; }
    public decimal Total { get; init; }

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
}