namespace Bilreg.Application.RadContext.RadOrderFeature.Integration;

public class RadBillingChargeException : Exception
{
    public RadBillingChargeException() { }
    public RadBillingChargeException(string message) : base(message) { }
    public RadBillingChargeException(string message, Exception innerException) : base(message, innerException) { }
}
