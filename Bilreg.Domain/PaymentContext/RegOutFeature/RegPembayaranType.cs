namespace Bilreg.Domain.PaymentContext.RegOutFeature;

public class RegPembayaranType 
{
    #region CREATION
    public RegPembayaranType(string caraBayarId, string caraBayarName, decimal nilaiJasa, decimal nilaiObat, decimal nilaiSubTotal)
    {
        CaraBayarId = caraBayarId;
        CaraBayarName = caraBayarName;
        NilaiJasa = nilaiJasa;
        NilaiObat = nilaiObat;
        NilaiSubTotal = nilaiSubTotal;
    }
    #endregion
    #region PROPERTIES
    public string CaraBayarId { get; init; }
    public string CaraBayarName { get; init; }
    public decimal NilaiJasa { get; init; }
    public decimal NilaiObat { get; init; }
    public decimal NilaiSubTotal { get; init; }
    #endregion
}
