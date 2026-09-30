namespace Bilreg.Domain.ChargeContext.Shared;

//  M03-F01 P2-S04 — concurrency exception for follow-up order state transitions.
//  Thrown when a late state write cannot overwrite a confirmed reception
//  because the RowVersion no longer matches the current DB value.
public class TindakanLanjutConcurrencyException : InvalidOperationException
{
    public TindakanLanjutConcurrencyException(string aggregateId, int expectedVersion, int currentVersion)
        : base(
            $"Concurrency conflict on follow-up order '{aggregateId}' expected version {expectedVersion} but current version is {currentVersion}.")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }

    public string AggregateId { get; }
    public int ExpectedVersion { get; }
    public int CurrentVersion { get; }
}