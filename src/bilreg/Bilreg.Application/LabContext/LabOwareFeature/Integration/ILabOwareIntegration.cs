namespace Bilreg.Application.LabContext.LabOwareFeature.Integration;

public interface ILabOwareIntegration
{
    bool IsActive { get; }
    LabOwareSendResult Send(string payloadJson);
}
