using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class OrderMutasiItemModel
{
    #region CREATION
    public OrderMutasiItemModel(int noUrut, BrgReff brg, decimal qty, SatuanType satuan, decimal stokCurrent = 0m, decimal qtyTerpenuhi = 0m)
    {
        NoUrut = noUrut;
        Brg = brg;
        Qty = qty;
        Satuan = satuan;
        StokCurrent = stokCurrent;
        QtyTerpenuhi = qtyTerpenuhi;
    }

    public static OrderMutasiItemModel Default
        => new(0, new BrgReff("-", "-"), 0, SatuanType.Default, 0m, 0m);
    #endregion

    #region PROPERTIES
    public int NoUrut { get; internal set; }
    public BrgReff Brg { get; private set; }
    public decimal Qty { get; private set; }
    public SatuanType Satuan { get; private set; }
    public decimal StokCurrent { get; private set; }
    public decimal QtyTerpenuhi { get; private set; }
    public decimal SisaBackorder => Math.Max(0m, Qty - QtyTerpenuhi);
    #endregion

    #region BEHAVIOUR
    internal void AddQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty harus lebih dari 0");

        Qty += qty;
    }

    internal void SetQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty harus lebih dari 0");

        Qty = qty;
    }

    internal void SetStokCurrent(decimal stokCurrent)
    {
        StokCurrent = stokCurrent;
    }

    internal void AddFulfilledQty(decimal qtyFulfilled)
    {
        if (qtyFulfilled <= 0)
            throw new ArgumentException("Qty pemenuhan harus lebih dari 0");

        if (QtyTerpenuhi + qtyFulfilled > Qty)
            throw new ArgumentException("Qty pemenuhan melebihi sisa permintaan");

        QtyTerpenuhi += qtyFulfilled;
    }

    internal void SetNoUrut(int noUrut)
    {
        if (noUrut <= 0)
            throw new ArgumentException("NoUrut invalid");

        NoUrut = noUrut;
    }
    #endregion
}
