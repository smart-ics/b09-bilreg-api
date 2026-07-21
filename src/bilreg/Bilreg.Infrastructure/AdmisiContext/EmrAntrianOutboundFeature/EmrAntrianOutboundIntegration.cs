using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;

namespace Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundIntegration : IEmrAntrianOutboundIntegration
{
    private readonly IAddAntrianEmrByBookingService _bookingService;
    private readonly IAddAntrianEmrByRegService _regService;

    public EmrAntrianOutboundIntegration(
        IAddAntrianEmrByBookingService bookingService,
        IAddAntrianEmrByRegService regService)
    {
        _bookingService = bookingService;
        _regService = regService;
    }

    public EmrAntrianSendResult Send(string messageType, string payloadJson)
    {
        return messageType switch
        {
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking =>
                _bookingService.Send(EmrAntrianOutboundPayloadBuilder.DeserializeBooking(payloadJson)),
            EmrAntrianOutboundQueueModel.MessageTypeAddReg =>
                _regService.Send(EmrAntrianOutboundPayloadBuilder.DeserializeReg(payloadJson)),
            _ => new EmrAntrianSendResult(false, $"Unknown message type: {messageType}")
        };
    }
}
