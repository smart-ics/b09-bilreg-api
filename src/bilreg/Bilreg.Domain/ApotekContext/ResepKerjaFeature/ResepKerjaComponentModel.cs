using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.ResepKerjaFeature;

public class ResepKerjaComponentModel
{
    public ResepKerjaComponentModel(
        int itemNo,
        int componentNo,
        string brgId,
        string brgName,
        string satuanId,
        decimal qty)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NegativeOrZero(componentNo, nameof(componentNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Component qty must be greater than zero.");

        ItemNo = itemNo;
        ComponentNo = componentNo;
        BrgId = brgId;
        BrgName = brgName ?? "";
        SatuanId = satuanId ?? "";
        Qty = qty;
    }

    public int ItemNo { get; }
    public int ComponentNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public string SatuanId { get; }
    public decimal Qty { get; }
}
