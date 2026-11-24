using System.Text.Json.Serialization;

namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransportTypeEnum
{
    InboundRetrieval = 1,
    OutboundReferral = 2,
    Discharge = 3,
    Hearse = 4
}
