using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.RegOutFeature;

public class RegBiayaType : IRegKey
{
    #region CREATION
    public RegBiayaType(string regId, string komponenId, string reffBlId, decimal nilai)
    {
        RegId = regId;
        KomponenId = komponenId;
        ReffBlId = reffBlId;
        Nilai = nilai;
    }
    #endregion
    #region PROPERTIES
    public string RegId { get; init; }
	public string KomponenId { get; init; }
	public string ReffBlId { get; init; }
	public decimal Nilai { get; init; }
    #endregion
}
