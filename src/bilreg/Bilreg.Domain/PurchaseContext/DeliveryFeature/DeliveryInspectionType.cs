namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

public record DeliveryInspectionType
{
    public DeliveryInspectionType(
        decimal qtyKirim,
        decimal qtyDiterima,
        decimal qtyDitolak,
        string alasanTolak,
        bool isBonus,
        bool hasJaminanRetur,
        string noSuratJaminan,
        string poId = "",
        int poNoUrut = 0)
    {
        QtyKirim = qtyKirim;
        QtyDiterima = qtyDiterima;
        QtyDitolak = qtyDitolak;
        AlasanTolak = alasanTolak ?? string.Empty;
        IsBonus = isBonus;
        HasJaminanRetur = hasJaminanRetur;
        NoSuratJaminan = noSuratJaminan ?? string.Empty;
        PoId = poId ?? string.Empty;
        PoNoUrut = poNoUrut;
    }

    public static DeliveryInspectionType Create(
        decimal qtyKirim,
        decimal qtyDiterima,
        decimal qtyDitolak,
        string alasanTolak,
        bool isBonus = false,
        bool hasJaminanRetur = false,
        string noSuratJaminan = "",
        string poId = "",
        int poNoUrut = 0)
    {
        if (qtyKirim < 0)
            throw new ArgumentException("Kuantitas kirim tidak boleh bernilai negatif.", nameof(qtyKirim));
        if (qtyDiterima < 0)
            throw new ArgumentException("Kuantitas diterima tidak boleh bernilai negatif.", nameof(qtyDiterima));
        if (qtyDitolak < 0)
            throw new ArgumentException("Kuantitas ditolak tidak boleh bernilai negatif.", nameof(qtyDitolak));

        if (qtyKirim != qtyDiterima + qtyDitolak)
            throw new ArgumentException(
                $"Kuantitas kirim ({qtyKirim}) harus sama dengan kuantitas diterima ({qtyDiterima}) + kuantitas ditolak ({qtyDitolak}).");

        if (qtyDitolak > 0 && string.IsNullOrWhiteSpace(alasanTolak))
            throw new ArgumentException("Alasan tolak wajib diisi apabila kuantitas ditolak lebih dari 0.", nameof(alasanTolak));

        if (hasJaminanRetur && string.IsNullOrWhiteSpace(noSuratJaminan))
            throw new ArgumentException("Nomor surat jaminan retur wajib diisi apabila memiliki jaminan retur.", nameof(noSuratJaminan));

        return new DeliveryInspectionType(
            qtyKirim,
            qtyDiterima,
            qtyDitolak,
            qtyDitolak > 0 ? alasanTolak.Trim() : (alasanTolak?.Trim() ?? string.Empty),
            isBonus,
            hasJaminanRetur,
            hasJaminanRetur ? noSuratJaminan.Trim() : (noSuratJaminan?.Trim() ?? string.Empty),
            poId?.Trim() ?? string.Empty,
            poNoUrut);
    }

    public decimal QtyKirim { get; init; }
    public decimal QtyDiterima { get; init; }
    public decimal QtyDitolak { get; init; }
    public string AlasanTolak { get; init; }
    public bool IsBonus { get; init; }
    public bool HasJaminanRetur { get; init; }
    public string NoSuratJaminan { get; init; }
    public string PoId { get; init; }
    public int PoNoUrut { get; init; }

    public static DeliveryInspectionType Default => new(0, 0, 0, string.Empty, false, false, string.Empty);
}
