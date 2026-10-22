namespace Bilreg.Domain.Shared;

/// <summary>
/// Domain exception thrown when a business rule validation fails and the ongoing
/// operation must be aborted. Dipetakan menjadi HTTP 400 (Bad Request) oleh
/// <c>ErrorHandlerMiddleware</c>.
/// </summary>
public class BusinessValidationException : InvalidOperationException
{
    public BusinessValidationException(string message)
        : base(message)
    {
    }

    public BusinessValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
