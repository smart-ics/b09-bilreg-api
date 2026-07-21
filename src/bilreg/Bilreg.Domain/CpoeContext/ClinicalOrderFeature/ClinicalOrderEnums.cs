namespace Bilreg.Domain.CpoeContext.ClinicalOrderFeature;

public enum ClinicalOrderStatusEnum
{
    Active,
    Completed,
    NotPerformed,
    Cancelled
}

public enum OrderOccurrenceStatusEnum
{
    Active,
    Completed,
    NotPerformed,
    Cancelled
}

public enum FulfilmentOutcomeEnum
{
    Completed,
    NotPerformed
}

public enum OrderHistoryActionEnum
{
    Created,
    IntentModified,
    OccurrencePlanModified,
    OccurrenceDestinationChanged,
    OccurrencePlannedExecutionTimeChanged,
    OccurrenceCompleted,
    OccurrenceNotPerformed,
    Cancelled
}
