using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

public record DeliveryOrderItemType
{
    public DeliveryOrderItemType(
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal subTotal,
        decimal taxPercentage,
        decimal tax,
        decimal total,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        decimal het = 0m)
    {
        if (het < 0)
            throw new ArgumentException("HET tidak boleh bernilai negatif.", nameof(het));

        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Konversi = konversi;
        Harga = harga;

        if (inspection?.IsBonus == true)
        {
            Diskon = harga;
            SubTotal = 0m;
            Tax = 0m;
            Total = 0m;
        }
        else
        {
            Diskon = diskon;
            SubTotal = subTotal;
            Tax = tax;
            Total = total;
        }

        TaxPercentage = taxPercentage;
        NoBatch = noBatch;
        TglEd = tglEd;
        Inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        Het = het;
    }

    public static DeliveryOrderItemType Create(
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        decimal het = 0m,
        DateTime? referenceDate = null)
    {
        if (brg == null || string.IsNullOrWhiteSpace(brg.BrgId))
            throw new ArgumentException("Barang wajib ditentukan.", nameof(brg));
        if (satuan == null || string.IsNullOrWhiteSpace(satuan.SatuanId))
            throw new ArgumentException("Satuan kemasan wajib ditentukan.", nameof(satuan));
        if (konversi <= 0)
            throw new ArgumentException("Faktor konversi satuan harus lebih besar dari 0.", nameof(konversi));
        if (harga < 0)
            throw new ArgumentException("Harga satuan tidak boleh bernilai negatif.", nameof(harga));
        if (diskon < 0)
            throw new ArgumentException("Diskon satuan tidak boleh bernilai negatif.", nameof(diskon));
        if (inspection == null)
            throw new ArgumentException("Data kendali mutu inspeksi wajib disertakan.", nameof(inspection));
        if (!inspection.IsBonus && diskon > harga)
            throw new ArgumentException("Diskon per satuan tidak boleh melebihi harga satuan.", nameof(diskon));
        if (taxPercentage < 0)
            throw new ArgumentException("Persentase pajak tidak boleh bernilai negatif.", nameof(taxPercentage));
        if (het < 0)
            throw new ArgumentException("HET tidak boleh bernilai negatif.", nameof(het));
        if (string.IsNullOrWhiteSpace(noBatch))
            throw new ArgumentException("Nomor lot/batch tidak boleh kosong.", nameof(noBatch));

        var today = (referenceDate ?? DateTime.Today).Date;
        if (tglEd.Date <= today)
            throw new ArgumentException(
                $"Tanggal kedaluwarsa ({tglEd:yyyy-MM-dd}) tidak boleh kurang dari atau sama dengan hari ini ({today:yyyy-MM-dd}).",
                nameof(tglEd));

        var threshold18Months = today.AddMonths(18);
        if (tglEd.Date < threshold18Months)
        {
            if (!inspection.HasJaminanRetur || string.IsNullOrWhiteSpace(inspection.NoSuratJaminan))
                throw new ArgumentException(
                    $"Barang dengan sisa masa simpan kurang dari 18 bulan (ED: {tglEd:yyyy-MM-dd}) wajib menyertakan surat jaminan retur vendor dan nomor surat jaminan.",
                    nameof(inspection));
        }

        decimal subTotal;
        decimal tax;
        decimal total;

        if (inspection.IsBonus)
        {
            diskon = harga;
            subTotal = 0m;
            tax = 0m;
            total = 0m;
        }
        else
        {
            subTotal = (harga - diskon) * inspection.QtyDiterima;
            tax = subTotal * taxPercentage / 100m;
            total = subTotal + tax;
        }

        return new DeliveryOrderItemType(
            noUrut,
            brg,
            satuan,
            konversi,
            harga,
            diskon,
            subTotal,
            taxPercentage,
            tax,
            total,
            noBatch.Trim(),
            tglEd,
            inspection,
            het);
    }

    public static DeliveryOrderItemType Create(
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        DateTime? referenceDate)
        => Create(
            noUrut,
            brg,
            satuan,
            konversi,
            harga,
            diskon,
            taxPercentage,
            noBatch,
            tglEd,
            inspection,
            0m,
            referenceDate);

    public static DeliveryOrderItemType Create(
        int noUrut,
        IBrg brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        decimal het = 0m,
        DateTime? referenceDate = null)
        => Create(
            noUrut,
            brg?.ToReff() ?? throw new ArgumentNullException(nameof(brg)),
            satuan,
            konversi,
            harga,
            diskon,
            taxPercentage,
            noBatch,
            tglEd,
            inspection,
            het,
            referenceDate);

    public static DeliveryOrderItemType Create(
        int noUrut,
        IBrg brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        DateTime? referenceDate)
        => Create(
            noUrut,
            brg?.ToReff() ?? throw new ArgumentNullException(nameof(brg)),
            satuan,
            konversi,
            harga,
            diskon,
            taxPercentage,
            noBatch,
            tglEd,
            inspection,
            0m,
            referenceDate);

    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public int Konversi { get; init; }
    public decimal Harga { get; init; }
    public decimal Diskon { get; init; }
    public decimal SubTotal { get; init; }
    public decimal TaxPercentage { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public string NoBatch { get; init; }
    public DateTime TglEd { get; init; }
    public DeliveryInspectionType Inspection { get; init; }
    public decimal Het { get; init; }

    public decimal Qty => Inspection.QtyDiterima;
    public decimal QtyDiterima => Inspection.QtyDiterima;
    public decimal Hpp => Inspection.IsBonus ? 0m : (Konversi > 0 ? (Harga - Diskon) / Konversi : 0m);

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
}
