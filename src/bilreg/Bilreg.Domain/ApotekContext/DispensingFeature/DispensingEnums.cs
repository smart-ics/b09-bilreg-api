namespace Bilreg.Domain.ApotekContext.DispensingFeature;

public enum DispensingStatusEnum
{
    Established = 0,
    AwaitingClearance = 1,
    Released = 2,
    Preparing = 3,
    Prepared = 4,
    Completed = 5,
    Cancelled = 6,
    Expired = 7,
    Unfulfilled = 8
}

public enum DispensingItemOutcomeEnum
{
    Open = 0,
    Dispensed = 1,
    Returned = 2,
    Unfulfilled = 3
}

public enum FinalReviewOutcomeEnum
{
    Pass = 0,
    Fail = 1
}

public interface IDispensingKey
{
    string DispensingId { get; }
}
