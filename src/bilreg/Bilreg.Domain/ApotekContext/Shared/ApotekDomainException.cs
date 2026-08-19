namespace Bilreg.Domain.ApotekContext.Shared;

public class ApotekDomainException : InvalidOperationException
{
    public ApotekDomainException(string message) : base(message)
    {
    }
}

public class ApotekConcurrencyException : ApotekDomainException
{
    public ApotekConcurrencyException(string aggregateId, int expectedVersion)
        : base($"Concurrency conflict on '{aggregateId}' expected version {expectedVersion}.")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
    }

    public string AggregateId { get; }
    public int ExpectedVersion { get; }
}
