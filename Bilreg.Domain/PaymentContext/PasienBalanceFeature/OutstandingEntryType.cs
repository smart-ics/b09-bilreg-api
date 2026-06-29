namespace Bilreg.Domain.PaymentContext.PasienBalanceFeature;

public record OutstandingEntryType
{
    public OutstandingEntryType(
        string entryId,
        string pasienId,
        string regId,
        decimal outstandingJasa,
        decimal outstandingObat,
        DateTime lastTransactionDate,
        string sourceReference,
        DateTime createdAt,
        string createdBy,
        bool isPersisted)
    {
        EntryId = entryId;
        PasienId = pasienId;
        RegId = regId ?? throw new ArgumentException("RegId tidak boleh kosong.", nameof(regId));
        OutstandingJasa = outstandingJasa;
        OutstandingObat = outstandingObat;
        LastTransactionDate = lastTransactionDate;
        SourceReference = sourceReference ?? string.Empty;
        CreatedAt = createdAt;
        CreatedBy = createdBy ?? string.Empty;
        IsPersisted = isPersisted;
    }

    public string EntryId { get; init; }
    public string PasienId { get; init; }
    public string RegId { get; init; }
    public decimal OutstandingJasa { get; init; }
    public decimal OutstandingObat { get; init; }
    public decimal OutstandingTotal => OutstandingJasa + OutstandingObat;
    public DateTime LastTransactionDate { get; init; }
    public string SourceReference { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
    public bool IsPersisted { get; init; }
}
