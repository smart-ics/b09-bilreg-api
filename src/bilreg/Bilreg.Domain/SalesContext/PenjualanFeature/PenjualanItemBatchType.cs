namespace Bilreg.Domain.SalesContext.PenjualanFeature;

public record PenjualanItemBatchType
{
    public PenjualanItemBatchType(
        string penjualanItemBatchId,
        int noUrut,
        string noBatch,
        DateTime tglEd,
        string poId,
        string doId,
        decimal qty,
        decimal hpp,
        bool isVoided)
    {
        PenjualanItemBatchId = penjualanItemBatchId;
        NoUrut = noUrut;
        NoBatch = noBatch;
        TglEd = tglEd;
        PoId = poId;
        DoId = doId;
        Qty = qty;
        Hpp = hpp;
        IsVoided = isVoided;
    }

    public string PenjualanItemBatchId { get; init; }
    public int NoUrut { get; private set; }
    public string NoBatch { get; init; }
    public DateTime TglEd { get; init; }
    public string PoId { get; init; }
    public string DoId { get; init; }
    public decimal Qty { get; init; }
    public decimal Hpp { get; init; }
    public bool IsVoided { get; private set; }

    public void VoidBatch() => IsVoided = true;
    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
}
