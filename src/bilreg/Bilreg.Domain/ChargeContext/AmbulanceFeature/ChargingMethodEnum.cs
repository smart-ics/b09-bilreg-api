using System.Text.Json.Serialization;

namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChargingMethodEnum
{
    Wilayah = 1,
    Jarak = 2
}
