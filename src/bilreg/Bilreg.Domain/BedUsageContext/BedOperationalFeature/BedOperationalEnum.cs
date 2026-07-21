namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public enum BedReadinessStatusEnum
{
    Ready = 1,
    CleaningRequired = 2,
    CleaningInProgress = 3,
    Blocked = 4,
    OutOfService = 5
}

public enum BedRestrictionTypeEnum
{
    None = 0,
    Cleaning = 1,
    Inspection = 2,
    Maintenance = 3,
    Safety = 4,
    Operational = 5
}
