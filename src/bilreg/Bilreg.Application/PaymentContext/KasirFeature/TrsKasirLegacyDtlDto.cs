namespace Bilreg.Application.PaymentContext.KasirFeature;

public record TrsKasirLegacyDtlDto
{
    public string TrsKasirId { get; init; } = string.Empty;
    public decimal NoUrut { get; init; }
    public string TrsBillId { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string Keterangan { get; init; } = string.Empty;
    public decimal NilaiBill { get; init; }
    public decimal NilaiKas { get; init; }
    public decimal NilaiBank { get; init; }
    public decimal NilaiKartuKredit { get; init; }
    public bool IsVoid { get; init; }
    public bool IsJasa { get; init; }
    public bool IsObat { get; init; }
    public bool IsTitipan { get; init; }
    public string TrsVoucherId { get; init; } = string.Empty;
    public decimal VoucherDep { get; init; }

    public TrsKasirLegacyDtlDto() { }

    public TrsKasirLegacyDtlDto(
        string trsKasirId,
        decimal noUrut,
        string trsBillId,
        string layananId,
        string keterangan,
        decimal nilaiBill,
        decimal nilaiKas,
        decimal nilaiBank,
        decimal nilaiKartuKredit,
        bool isVoid = false,
        bool isJasa = false,
        bool isObat = false,
        bool isTitipan = false,
        string trsVoucherId = "",
        decimal voucherDep = 0)
    {
        TrsKasirId = trsKasirId;
        NoUrut = noUrut;
        TrsBillId = trsBillId;
        LayananId = layananId;
        Keterangan = keterangan;
        NilaiBill = nilaiBill;
        NilaiKas = nilaiKas;
        NilaiBank = nilaiBank;
        NilaiKartuKredit = nilaiKartuKredit;
        IsVoid = isVoid;
        IsJasa = isJasa;
        IsObat = isObat;
        IsTitipan = isTitipan;
        TrsVoucherId = trsVoucherId;
        VoucherDep = voucherDep;
    }
}
