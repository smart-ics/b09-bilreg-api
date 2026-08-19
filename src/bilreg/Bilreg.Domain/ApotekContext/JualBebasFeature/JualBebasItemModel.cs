using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.JualBebasFeature;

public class JualBebasItemModel
{
    public JualBebasItemModel(int itemNo, string brgId, string brgName, string satuanId, decimal qty, string signa)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(brgName, nameof(brgName));
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty));

        ItemNo = itemNo;
        BrgId = brgId;
        BrgName = brgName;
        SatuanId = satuanId ?? "";
        Qty = qty;
        Signa = signa ?? "";
    }

    public int ItemNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public string SatuanId { get; }
    public decimal Qty { get; }
    public string Signa { get; }
}
