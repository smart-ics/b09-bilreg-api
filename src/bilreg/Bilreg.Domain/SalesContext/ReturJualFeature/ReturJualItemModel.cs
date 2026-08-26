using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public record ReturJualItemModel
{
    public ReturJualItemModel(
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

    public static ReturJualItemModel Create(
        string returJualItemId,
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        decimal qtyJual,
        decimal qtyRetur,
        NilaiItemReturType nilai)
        => new(returJualItemId, noUrut, brg, satuan, qtyJual, qtyRetur, nilai, false);

    internal void SetNoUrut(int noUrut) => NoUrut = noUrut;

    internal void ApplyNilai(decimal hargaRetur, decimal taxPerUnit)
    {
        Nilai = NilaiItemReturType.Create(QtyJual, QtyRetur, Nilai.HargaJual, hargaRetur, taxPerUnit);
    }

    internal void ChangeQtyRetur(decimal qtyRetur) => QtyRetur = qtyRetur;

    internal void VoidLine() => IsVoided = true;
}
