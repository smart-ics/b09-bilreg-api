namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public class AdmissionQueueConcurrencyException : InvalidOperationException
{
    public AdmissionQueueConcurrencyException(string message) : base(message)
    {
    }
}
