namespace Bilreg.Application.PaymentContext.KasirFeature;

public record TrsKasirLegacyHdrDto
{
    public string TrsKasirId { get; init; } = string.Empty;
    public string TglTrs { get; init; } = string.Empty;
    public string JamTrs { get; init; } = string.Empty;
    public string PetugasId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public decimal SumKas { get; init; }
    public decimal SumBank { get; init; }
    public decimal SumKartuKredit { get; init; }
    public decimal SumAll { get; init; }
    public string KdRekKas { get; init; } = string.Empty;
    public string KdRekBank { get; init; } = string.Empty;
    public string TglVoid { get; init; } = "3000-01-01";
    public string JamVoid { get; init; } = "00:00:00";
    public string PetugasVoid { get; init; } = string.Empty;
    public string KdRekKartuKredit { get; init; } = string.Empty;
    public string NoKartuKredit { get; init; } = string.Empty;
    public string KdTrsKwitansi { get; init; } = string.Empty;
    public string KdTrsCharge { get; init; } = string.Empty;
    public string KdTrsDiscKredit { get; init; } = string.Empty;
    public string KdTrsChargeBank { get; init; } = string.Empty;
    public string KdTrsDiscBank { get; init; } = string.Empty;
    public string KdJenisBank { get; init; } = string.Empty;
    public string KdJenisKartuKredit { get; init; } = string.Empty;
    public string NoBank { get; init; } = string.Empty;
    public string KdRekDeposit { get; init; } = "4210000";
    public decimal GenKeu { get; init; } = 5;
    public decimal StatusBridge { get; init; }
    public decimal SumVoucherDep { get; init; }
    public string CrtTgl { get; init; } = string.Empty;
    public string CrtJam { get; init; } = string.Empty;
    public string CrtIpa { get; init; } = "127.0.0.1";
    public string CrtVer { get; init; } = "1.0";
    public string CrtUsr { get; init; } = string.Empty;
    public string UpdTgl { get; init; } = string.Empty;
    public string UpdJam { get; init; } = string.Empty;
    public string UpdIpa { get; init; } = string.Empty;
    public string UpdVer { get; init; } = string.Empty;
    public string UpdUsr { get; init; } = string.Empty;
    public string TglJamTrs { get; init; } = string.Empty;

    public TrsKasirLegacyHdrDto() { }

    public TrsKasirLegacyHdrDto(
        string trsKasirId,
        string tglTrs,
        string jamTrs,
        string petugasId,
        string regId,
        decimal sumKas,
        decimal sumBank,
        decimal sumKartuKredit,
        decimal sumAll,
        string kdRekKas,
        string kdRekBank,
        string tglVoid = "3000-01-01",
        string jamVoid = "00:00:00",
        string petugasVoid = "",
        string kdRekKartuKredit = "",
        string noKartuKredit = "",
        string kdTrsKwitansi = "",
        string kdTrsCharge = "",
        string kdTrsDiscKredit = "",
        string kdTrsChargeBank = "",
        string kdTrsDiscBank = "",
        string kdJenisBank = "",
        string kdJenisKartuKredit = "",
        string noBank = "",
        string kdRekDeposit = "4210000",
        decimal genKeu = 5,
        decimal statusBridge = 0,
        decimal sumVoucherDep = 0,
        string crtTgl = "",
        string crtJam = "",
        string crtIpa = "127.0.0.1",
        string crtVer = "1.0",
        string crtUsr = "",
        string updTgl = "",
        string updJam = "",
        string updIpa = "",
        string updVer = "",
        string updUsr = "",
        string tglJamTrs = "")
    {
        TrsKasirId = trsKasirId;
        TglTrs = tglTrs;
        JamTrs = jamTrs;
        PetugasId = petugasId;
        RegId = regId;
        SumKas = sumKas;
        SumBank = sumBank;
        SumKartuKredit = sumKartuKredit;
        SumAll = sumAll;
        KdRekKas = kdRekKas;
        KdRekBank = kdRekBank;
        TglVoid = tglVoid;
        JamVoid = jamVoid;
        PetugasVoid = petugasVoid;
        KdRekKartuKredit = kdRekKartuKredit;
        NoKartuKredit = noKartuKredit;
        KdTrsKwitansi = kdTrsKwitansi;
        KdTrsCharge = kdTrsCharge;
        KdTrsDiscKredit = kdTrsDiscKredit;
        KdTrsChargeBank = kdTrsChargeBank;
        KdTrsDiscBank = kdTrsDiscBank;
        KdJenisBank = kdJenisBank;
        KdJenisKartuKredit = kdJenisKartuKredit;
        NoBank = noBank;
        KdRekDeposit = kdRekDeposit;
        GenKeu = genKeu;
        StatusBridge = statusBridge;
        SumVoucherDep = sumVoucherDep;
        CrtTgl = crtTgl;
        CrtJam = crtJam;
        CrtIpa = crtIpa;
        CrtVer = crtVer;
        CrtUsr = crtUsr;
        UpdTgl = updTgl;
        UpdJam = updJam;
        UpdIpa = updIpa;
        UpdVer = updVer;
        UpdUsr = updUsr;
        TglJamTrs = tglJamTrs;
    }
}
