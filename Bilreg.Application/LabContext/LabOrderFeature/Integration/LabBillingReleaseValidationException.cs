namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

/// <summary>
/// Infrastructure/runtime failure when calling BIL release validation (not operational BLOCKED).
/// </summary>
public class LabBillingReleaseValidationException : Exception
{
    public LabBillingReleaseValidationException(string message)
        : base(message)
    {
    }

    public LabBillingReleaseValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
