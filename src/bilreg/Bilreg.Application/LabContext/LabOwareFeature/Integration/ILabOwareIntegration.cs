namespace Bilreg.Application.LabContext.LabOwareFeature.Integration;

public interface ILabOwareIntegration
{
    LabOwareSendResult Send(string payloadJson);
}
