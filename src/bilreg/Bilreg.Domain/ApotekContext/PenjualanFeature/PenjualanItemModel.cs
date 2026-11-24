using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.PenjualanFeature;

public class PenjualanItemModel
{
    public int NoUrut { get; private set; }
    public string PenjualanItemId { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public string SatuanId { get; }
    public string SatuanName { get; }
    public decimal Qty { get; }
    public decimal HargaSatuan { get; }
    public decimal Diskon { get; }
    public decimal Biaya { get; } // tuslah /r
    public decimal SubTotal { get; }
    public decimal TaxProsen { get; }
    public decimal TaxRupiah { get; }
    public decimal Fee { get; } // jasa racik
    public decimal Total { get; }
    public string Etiket { get; }
    public decimal EtiketQty { get; }
    public decimal EtiketHari { get; }
    public string EtiketKdPakai { get; }
    public string EtiketCatatan { get; }
    public bool IsRacik { get; }
    public bool IsKomponen { get; }
    public string RacikId { get; }
    public bool IsBeliBebas { get; private set; }
    public decimal NilaiKlaim { get; }
    public bool IsVoid { get; private set; }

    public PenjualanItemModel(
        int noUrut,
        string penjualanItemId,
        string brgId,
        string brgName,
        string satuanId,
        string satuanName,
        decimal qty,
        decimal hargaSatuan,
        decimal diskon,
        decimal biaya,
        decimal taxProsen,
        decimal taxRupiah,
        decimal fee,
        decimal total,
        string etiket,
        decimal etiketQty,
        decimal etiketHari,
        string etiketKdPakai,
        string etiketCatatan,
        bool isRacik,
        bool isKomponen,
        string racikId,
        bool isBeliBebas,
        decimal nilaiKlaim,
        bool isVoid = false,
        decimal? subTotal = null)
    {
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        Guard.Against.NullOrWhiteSpace(penjualanItemId, nameof(penjualanItemId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.Null(brgName, nameof(brgName));
        Guard.Against.Null(satuanId, nameof(satuanId));
        Guard.Against.Null(satuanName, nameof(satuanName));

        if (qty <= 0)
            throw new ApotekDomainException("Kuantitas barang (Qty) harus lebih besar dari nol.");

        Guard.Against.Negative(hargaSatuan, nameof(hargaSatuan));
        Guard.Against.Negative(diskon, nameof(diskon));
        Guard.Against.Negative(biaya, nameof(biaya));
        Guard.Against.Negative(taxProsen, nameof(taxProsen));
        Guard.Against.Negative(taxRupiah, nameof(taxRupiah));
        Guard.Against.Negative(fee, nameof(fee));
        Guard.Against.Negative(total, nameof(total));
        Guard.Against.Negative(nilaiKlaim, nameof(nilaiKlaim));
        Guard.Against.Negative(etiketQty, nameof(etiketQty));
        Guard.Against.Negative(etiketHari, nameof(etiketHari));

        if (isRacik && isKomponen)
            throw new ApotekDomainException("Baris item tidak boleh menjadi sediaan racikan sekaligus komponen racikan.");

        if (isKomponen && string.IsNullOrWhiteSpace(racikId))
            throw new ApotekDomainException("Komponen racikan wajib mencantumkan RacikId induk.");

        NoUrut = noUrut;
        PenjualanItemId = penjualanItemId;
        BrgId = brgId;
        BrgName = brgName;
        SatuanId = satuanId;
        SatuanName = satuanName;
        Qty = qty;
        HargaSatuan = hargaSatuan;
        Diskon = diskon;
        Biaya = biaya;
        SubTotal = subTotal ?? ((qty * hargaSatuan) - diskon);
        TaxProsen = taxProsen;
        TaxRupiah = taxRupiah;
        Fee = fee;
        Total = total;
        Etiket = etiket ?? "";
        EtiketQty = etiketQty;
        EtiketHari = etiketHari;
        EtiketKdPakai = etiketKdPakai ?? "";
        EtiketCatatan = etiketCatatan ?? "";
        IsRacik = isRacik;
        IsKomponen = isKomponen;
        RacikId = racikId ?? "";
        IsBeliBebas = isBeliBebas;
        NilaiKlaim = nilaiKlaim;
        IsVoid = isVoid;
    }

    public static PenjualanItemModel Create(
        int noUrut,
        string penjualanItemId,
        string brgId,
        string brgName,
        string satuanId,
        string satuanName,
        decimal qty,
        decimal hargaSatuan,
        decimal diskon = 0,
        decimal biaya = 0,
        decimal taxProsen = 0,
        decimal taxRupiah = 0,
        decimal fee = 0,
        string etiket = "",
        decimal etiketQty = 0,
        decimal etiketHari = 0,
        string etiketKdPakai = "",
        string etiketCatatan = "",
        bool isRacik = false,
        bool isKomponen = false,
        string racikId = "",
        bool isBeliBebas = false,
        decimal nilaiKlaim = 0)
    {
        var subTotal = (qty * hargaSatuan) - diskon;
        var computedTaxRupiah = taxRupiah > 0 ? taxRupiah : (taxProsen > 0 ? Math.Round(subTotal * taxProsen / 100m, 2) : 0m);
        var total = subTotal + biaya + computedTaxRupiah + fee;

        return new PenjualanItemModel(
            noUrut,
            penjualanItemId,
            brgId,
            brgName,
            satuanId,
            satuanName,
            qty,
            hargaSatuan,
            diskon,
            biaya,
            taxProsen,
            computedTaxRupiah,
            fee,
            total,
            etiket,
            etiketQty,
            etiketHari,
            etiketKdPakai,
            etiketCatatan,
            isRacik,
            isKomponen,
            racikId,
            isBeliBebas,
            nilaiKlaim,
            isVoid: false,
            subTotal: subTotal);
    }

    public void Void() => IsVoid = true;

    public void SetNoUrut(int noUrut)
    {
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        NoUrut = noUrut;
    }

    public void MarkBeliBebas() => IsBeliBebas = true;
}
