using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public record ReturJualItemType
{
    public ReturJualItemType(
        string returJualItemId,
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        decimal qtyJual,
        decimal qtyRetur,
        NilaiItemReturType nilai,
        bool isVoided)
    {
        ReturJualItemId = returJualItemId;
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        QtyJual = qtyJual;
        QtyRetur = qtyRetur;
        Nilai = nilai;
        IsVoided = isVoided;
    }

    public string ReturJualItemId { get; init; }
    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public decimal QtyJual { get; init; }
    public decimal QtyRetur { get; private set; }
    public NilaiItemReturType Nilai { get; private set; }
    public bool IsVoided { get; private set; }

    public static ReturJualItemType Create(
        string returJualItemId,
        int noUrut,
        IBrg brg,
        SatuanType satuan,
        decimal qtyJual,
        decimal qtyRetur,
        NilaiItemReturType nilai)
        => new(returJualItemId, noUrut, brg.ToReff(),satuan, qtyJual, qtyRetur, nilai, false);

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;

    public void ApplyNilai(NilaiItemReturType nilai) => Nilai = nilai;

    public void VoidLine() => IsVoided = true;
}
