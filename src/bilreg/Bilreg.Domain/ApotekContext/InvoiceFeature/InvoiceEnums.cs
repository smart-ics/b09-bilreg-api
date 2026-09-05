namespace Bilreg.Domain.ApotekContext.InvoiceFeature;

public enum InvoiceStatusEnum
{
    Established = 0,
    Issued = 1,
    FinanciallyCleared = 2,
    AdjustedOrCredited = 3,
    Resolved = 4,
    Cancelled = 5
}

public enum InvoiceItemKindEnum
{
    Medication = 0,
    Bhp = 1
}

public enum InvoiceCorrectionDispositionEnum
{
    DirectRevisionAllowed = 0,
    ManualTataRekeningCorrectionPending = 1,
    Correlated = 2,
    NotApplicable = 3
}

public interface IInvoiceKey
{
    string InvoiceId { get; }
}
