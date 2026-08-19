namespace Bilreg.Domain.ApotekContext.SalesOrderFeature;

public enum SalesOrderSourceKindEnum
{
    ResepKerja = 0,
    JualBebas = 1
}

public enum PayerPathEnum
{
    GeneralPatientPay = 0,
    Bpjs = 1,
    OtherInsurance = 2
}

public enum PartialReasonEnum
{
    None = 0,
    PatientRequest = 1,
    StockShortage = 2,
    FornasNotCovered = 3
}

public enum SalesOrderStatusEnum
{
    Established = 0,
    Active = 1,
    Resolved = 2,
    Cancelled = 3
}

public enum SalesOrderResolvedReasonEnum
{
    None = 0,
    FullyFulfilled = 1,
    CollectionWindowExpired = 2,
    PatientDeclined = 3,
    Cancelled = 4
}

public enum SalesOrderItemStatusEnum
{
    Active = 0,
    FullyInvoiced = 1,
    FullyFulfilled = 2,
    Cancelled = 3,
    Unfulfilled = 4
}

public enum FornasCoverageEnum
{
    Unknown = 0,
    Covered = 1,
    NotCovered = 2
}

public enum UnfulfilledReasonEnum
{
    StockShortageAfterEstablishment = 0,
    PatientDecline = 1,
    CollectionWindowExpired = 2,
    Cancellation = 3,
    PreEstablishmentExclusion = 4
}
