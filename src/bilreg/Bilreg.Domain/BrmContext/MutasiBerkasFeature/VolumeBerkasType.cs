namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public record VolumeBerkasType
{
    public decimal QtyPinjamRi { get; init; }
    public decimal QtyPinjamRj { get; init; }
    public decimal QtyKembaliRi { get; init; }
    public decimal QtyKembaliRj { get; init; }

    public decimal TotalPinjam => QtyPinjamRi + QtyPinjamRj;
    public decimal TotalKembali => QtyKembaliRi + QtyKembaliRj;
    public decimal SisaPinjam => TotalPinjam - TotalKembali;
    public bool IsLengkapKembali => TotalPinjam > 0 && SisaPinjam <= 0;

    public VolumeBerkasType(decimal qtyPinjamRi, decimal qtyPinjamRj, decimal qtyKembaliRi, decimal qtyKembaliRj)
    {
        if (qtyPinjamRi < 0)
            throw new ArgumentException("Jumlah berkas pinjam rawat inap tidak boleh negatif.", nameof(qtyPinjamRi));
        if (qtyPinjamRj < 0)
            throw new ArgumentException("Jumlah berkas pinjam rawat jalan tidak boleh negatif.", nameof(qtyPinjamRj));
        if (qtyKembaliRi < 0)
            throw new ArgumentException("Jumlah berkas kembali rawat inap tidak boleh negatif.", nameof(qtyKembaliRi));
        if (qtyKembaliRj < 0)
            throw new ArgumentException("Jumlah berkas kembali rawat jalan tidak boleh negatif.", nameof(qtyKembaliRj));
        if (qtyKembaliRi > qtyPinjamRi)
            throw new ArgumentException("Jumlah berkas kembali rawat inap melebihi jumlah pinjam.", nameof(qtyKembaliRi));
        if (qtyKembaliRj > qtyPinjamRj)
            throw new ArgumentException("Jumlah berkas kembali rawat jalan melebihi jumlah pinjam.", nameof(qtyKembaliRj));

        QtyPinjamRi = qtyPinjamRi;
        QtyPinjamRj = qtyPinjamRj;
        QtyKembaliRi = qtyKembaliRi;
        QtyKembaliRj = qtyKembaliRj;
    }

    public static VolumeBerkasType Default => new(0m, 0m, 0m, 0m);

    public static VolumeBerkasType CreatePinjam(decimal qtyRi, decimal qtyRj) =>
        new(qtyRi, qtyRj, 0m, 0m);

    public VolumeBerkasType CatatKembali(decimal qtyKembaliRi, decimal qtyKembaliRj) =>
        new(QtyPinjamRi, QtyPinjamRj, qtyKembaliRi, qtyKembaliRj);
}
