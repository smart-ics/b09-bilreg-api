namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public class LabBillingChargeException : Exception
{
    public LabBillingChargeException()
    {
    }

    public LabBillingChargeException(string message) : base(message)
    {
    }

    public LabBillingChargeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
