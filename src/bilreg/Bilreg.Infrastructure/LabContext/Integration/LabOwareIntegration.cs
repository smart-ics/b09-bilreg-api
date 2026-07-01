using Bilreg.Application.LabContext.LabOwareFeature.Integration;

namespace Bilreg.Infrastructure.LabContext.Integration;

public class LabOwareIntegration : ILabOwareIntegration
{
    public LabOwareSendResult Send(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new LabOwareSendResult(false, "Payload kosong.");

        if (payloadJson.Contains("\"FAIL\"", StringComparison.OrdinalIgnoreCase)
            || payloadJson.Contains("OWARE_FAIL", StringComparison.OrdinalIgnoreCase))
            return new LabOwareSendResult(false, "Simulated OWARE failure.");

        return new LabOwareSendResult(true, null);
    }
}
