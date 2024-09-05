using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;

namespace Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;

public class TujuanTransportModel(string id, string name, decimal konstanta, bool isPerkiraan): ITujuanTransportKey
{
    // PROPERTIES
    public string TujuanTransportId { get; protected set; } = id;
    public string TujuanTransportName { get; protected set; } = name;
    public decimal Konstanta { get; protected set; } = konstanta;
    public bool IsPerkiraan { get; protected set; } = isPerkiraan;
    public string DefaultAmbulanceId { get; protected set; } = string.Empty;

    // BEHAVIOUR
    public void SetName(string name) => TujuanTransportName = name;
    public void SetKonstanta(decimal konstanta) => Konstanta = konstanta;
    public void SetPerkiraan() => IsPerkiraan = true;
    public void UnSetPerkiraan() => IsPerkiraan = false;
    public void SetDefaultAmbulance(AmbulanceModel ambulance) => DefaultAmbulanceId = ambulance.AmbulanceId;
    public void UnSetDefaultAmbulance() => DefaultAmbulanceId = string.Empty;
}

public interface ITujuanTransportKey
{
    string TujuanTransportId { get; }
}