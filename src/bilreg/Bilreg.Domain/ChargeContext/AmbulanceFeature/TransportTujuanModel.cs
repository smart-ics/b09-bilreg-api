namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

public class TransportTujuanModel(string ambulanceId, string tujuanTransportId, decimal konstanta = 0) : IAmbulanceKey, ITujuanTransportKey
{
    public string AmbulanceId { get; protected set; } = ambulanceId;
    public string TujuanTransportId { get; protected set; } = tujuanTransportId;
    public decimal Konstanta { get; protected set; } = konstanta;

    public void SetKonstanta(decimal konstanta) => Konstanta = konstanta;
    public void SetAmbulanceId(string id) => AmbulanceId = id;
    public void SetTujuanTransportId(string id) => TujuanTransportId = id;
}
