namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public enum ExecutionSourceEnum
{
    Ordered,
    AdHoc,
    Independent
}

public enum RnaServiceWorkStatusEnum
{
    Pending,
    Assigned,
    Executed,
    Cancelled,
    Withdrawn,
    EnteredInError
}

public enum BillableClassificationEnum
{
    Billable,
    NonBillable
}

public enum SourceRevisionKindEnum
{
    Amended,
    Cancelled,
    Discontinued
}

public enum ExecutionCorrectionKindEnum
{
    Corrected,
    Replaced,
    EnteredInError
}

public enum SubsequentAuthorizationStatusEnum
{
    NotRequired,
    Required,
    Acknowledged,
    Authorized,
    Overdue,
    LateReviewed
}
