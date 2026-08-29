namespace Bilreg.Domain.ApotekContext.IntegrationFeature;

public enum AptIntegrationTaskTypeEnum
{
    TrackerServedAt = 1,
    TrackerDoneAtPickup = 2,
    TrackerDoneAtNoShow = 3,
    TrackerWithdrawn = 4,
    StockReserve = 5,
    StockRemoveOnHandover = 6,
    StockReturnNoShow = 7,
    BillingCharge = 8,
    IterConsume = 9,
    EmrRealization = 10
}
