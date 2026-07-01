using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.Shared.Helpers;

namespace Farinv.Domain.InventoryContext.MutasiFeature;

public class OrderMutasiItemModel
{
    #region CREATION
    public OrderMutasiItemModel(int noUrut, BrgReff brg, decimal qty, SatuanType satuan)
    {
        NoUrut = noUrut;
        Brg = brg;
        Qty = qty;
        Satuan = satuan;
    }

    public static OrderMutasiItemModel Default
        => new(0, new BrgReff("-", "-"), 0, SatuanType.Default);
    #endregion

    #region PROPERTIES
    public int NoUrut { get; internal set; }
    public BrgReff Brg { get; private set; }
    public decimal Qty { get; private set; }
    public SatuanType Satuan { get; private set; }
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
    internal void SetNoUrut(int noUrut)
    {
        if (noUrut <= 0)
            throw new ArgumentException("NoUrut invalid");

        NoUrut = noUrut;
    }
    #endregion
}
