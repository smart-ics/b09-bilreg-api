using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.RegOutFeature;

public class RegHutangType : IRegKey
{
    #region CREATION
    public RegHutangType(string regId, DateOnly regHutDate, decimal nilaiHutang, decimal nilaiSisa, decimal nilaiJasa, decimal nilaiObat)
    {
        RegId = regId;
        RegHutDate = regHutDate;
        NilaiHutang = nilaiHutang;
        NilaiSisa = nilaiSisa;
        NilaiJasa = nilaiJasa;
        NilaiObat = nilaiObat;
    }
    #endregion

    #region PROPERTIES
    public string RegId { get; init; }
    public DateOnly RegHutDate { get; init; }
    public decimal NilaiHutang { get; init; }
    public decimal NilaiSisa { get; init; }
    public decimal NilaiJasa { get; init; }
    public decimal NilaiObat { get; init; }
    #endregion
}
