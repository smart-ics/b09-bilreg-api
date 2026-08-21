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
}