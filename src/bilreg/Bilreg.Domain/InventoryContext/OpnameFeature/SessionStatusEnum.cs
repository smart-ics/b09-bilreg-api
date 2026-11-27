namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public enum SessionStatusEnum
{
    Draft = 0,
    Counting = 1,
    WaitingVerification = 2,
    Verifying = 3,
    WaitingApproval = 4,
    Approved = 5,
    Rejected = 6,
    Cancelled = 7
}
