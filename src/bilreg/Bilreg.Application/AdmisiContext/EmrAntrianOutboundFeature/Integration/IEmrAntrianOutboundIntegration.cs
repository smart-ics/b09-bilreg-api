namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;

public interface IEmrAntrianOutboundIntegration
{
    EmrAntrianSendResult Send(string messageType, string payloadJson);
}
