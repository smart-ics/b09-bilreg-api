namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Domain exception thrown when business integrity invariants are violated.
/// </summary>
public class DomainViolationException : InvalidOperationException
{
    public DomainViolationException(string message)
        : base(message)
    {
    }

    public DomainViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
