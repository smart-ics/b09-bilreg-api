using System.Text.Json;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;

public static class EmrAntrianOutboundPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string SerializeBooking(AddAntrianEmrByBookingCmd cmd)
        => JsonSerializer.Serialize(cmd, JsonOptions);

    public static string SerializeReg(AddAntrianEmrByRegCommand cmd)
        => JsonSerializer.Serialize(cmd, JsonOptions);

    public static AddAntrianEmrByBookingCmd DeserializeBooking(string payloadJson)
        => JsonSerializer.Deserialize<AddAntrianEmrByBookingCmd>(payloadJson, JsonOptions)
           ?? throw new InvalidOperationException("Payload AddBooking tidak valid.");

    public static AddAntrianEmrByRegCommand DeserializeReg(string payloadJson)
        => JsonSerializer.Deserialize<AddAntrianEmrByRegCommand>(payloadJson, JsonOptions)
           ?? throw new InvalidOperationException("Payload AddReg tidak valid.");
}
