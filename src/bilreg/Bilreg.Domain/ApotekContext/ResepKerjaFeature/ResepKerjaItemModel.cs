using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.ResepKerjaFeature;

public class ResepKerjaItemModel
{
    public ResepKerjaItemModel(
        int itemNo,
        int sourceItemNo,
        string brgId,
        string brgName,
        string satuanId,
        string satuanName,
        decimal qty,
        int iter,
        string signa,
        string instruction,
        string note,
        bool isRacik)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(brgName, nameof(brgName));
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Qty must be greater than zero.");

        ItemNo = itemNo;
        SourceItemNo = sourceItemNo;
        BrgId = brgId;
        BrgName = brgName;
        SatuanId = satuanId ?? "";
        SatuanName = satuanName ?? "";
        Qty = qty;
        Iter = iter;
        Signa = signa ?? "";
        Instruction = instruction ?? "";
        Note = note ?? "";
        IsRacik = isRacik;
    }

    public int ItemNo { get; }
    public int SourceItemNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public string SatuanId { get; }
    public string SatuanName { get; }
    public decimal Qty { get; }
    public int Iter { get; }
    public string Signa { get; }
    public string Instruction { get; }
    public string Note { get; }
    public bool IsRacik { get; }
}
