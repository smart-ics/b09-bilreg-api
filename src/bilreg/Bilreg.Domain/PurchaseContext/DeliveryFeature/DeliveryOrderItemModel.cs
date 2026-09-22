namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// One item receipt line of a Delivery Order.
/// QtyReceived accumulates across receipt installments and never exceeds QtyOrder.
/// </summary>
public class DeliveryOrderItemModel
{
    private const int EmptyDateYear = 3000;

    #region CREATION

    public DeliveryOrderItemModel(
        int itemNo,
        string brgId,
        string layananId,
        decimal qtyOrder,
        string satuanId,
        decimal harga,
        decimal diskon,
        decimal tax,
        DateTime tglEd,
        string noBatch)
    {
        ItemNo = itemNo;
        BrgId = brgId;
        LayananId = layananId;
        QtyOrder = qtyOrder;
        QtyReceived = 0;
        SatuanId = satuanId;
        Harga = harga;
        Diskon = diskon;
        Tax = tax;
        TglEd = tglEd;
        NoBatch = noBatch;
        State = DeliveryOrderItemStateEnum.Open;
    }

    public static DeliveryOrderItemModel Create(
        string brgId,
        string layananId,
        decimal qtyOrder,
        string satuanId,
        decimal harga,
        decimal diskon,
        decimal tax,
        DateTime? tglEd = null,
        string? noBatch = null)
    {
        if (string.IsNullOrWhiteSpace(brgId))
            throw new ArgumentException("BrgId wajib diisi", nameof(brgId));
        if (string.IsNullOrWhiteSpace(layananId))
            throw new ArgumentException("LayananId wajib diisi", nameof(layananId));
        if (qtyOrder <= 0)
            throw new ArgumentException("QtyOrder harus lebih dari 0", nameof(qtyOrder));
        if (harga < 0)
            throw new ArgumentException("Harga tidak boleh negatif", nameof(harga));
        if (diskon < 0)
            throw new ArgumentException("Diskon tidak boleh negatif", nameof(diskon));
        if (tax < 0)
            throw new ArgumentException("Tax tidak boleh negatif", nameof(tax));

        return new DeliveryOrderItemModel(
            0,
            brgId,
            layananId,
            qtyOrder,
            satuanId ?? string.Empty,
            harga,
            diskon,
            tax,
            tglEd ?? new DateTime(EmptyDateYear, 1, 1),
            noBatch ?? string.Empty);
    }

    public static DeliveryOrderItemModel Default
        => new(0, "-", "-", 0, "-", 0, 0, 0, new DateTime(EmptyDateYear, 1, 1), string.Empty);

    #endregion

    #region PROPERTIES

    public int ItemNo { get; internal set; }
    public string BrgId { get; private set; }
    public string LayananId { get; private set; }
    public decimal QtyOrder { get; private set; }
    public decimal QtyReceived { get; private set; }
    public string SatuanId { get; private set; }
    public decimal Harga { get; private set; }
    public decimal Diskon { get; private set; }
    public decimal Tax { get; private set; }
    public DateTime TglEd { get; private set; }
    public string NoBatch { get; private set; }
    public DeliveryOrderItemStateEnum State { get; private set; }

    public decimal QtyRemaining => QtyOrder - QtyReceived;

    #endregion

    #region BEHAVIOUR

    /// <summary>
    /// Unit cost derived from line Harga/Diskon/Tax by the configured MetodePersediaanHPP.
    /// </summary>
    public decimal ComputeHpp(HppMethodEnum method) =>
        DeliveryOrderHppCalculator.Calculate(method, Harga, Diskon, Tax);

    internal void Receive(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentException("Qty penerimaan harus lebih dari 0", nameof(qty));
        if (qty > QtyRemaining)
            throw new ArgumentException("Qty penerimaan melebihi sisa DO line", nameof(qty));

        QtyReceived += qty;
        State = QtyReceived == QtyOrder
            ? DeliveryOrderItemStateEnum.Received
            : DeliveryOrderItemStateEnum.Partial;
    }

    internal void SetQtyOrder(decimal qtyOrder)
    {
        if (qtyOrder <= 0)
            throw new ArgumentException("QtyOrder harus lebih dari 0", nameof(qtyOrder));

        QtyOrder = qtyOrder;
    }

    internal void SetItemNo(int itemNo)
    {
        if (itemNo <= 0)
            throw new ArgumentException("ItemNo invalid", nameof(itemNo));

        ItemNo = itemNo;
    }

    #endregion
}