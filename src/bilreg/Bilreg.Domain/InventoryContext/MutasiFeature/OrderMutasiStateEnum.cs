namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public enum OrderMutasiStateEnum
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4,
    PartiallyFulfilled = 5,
    ForceClosed = 6,
    Cancelled = 7
}
