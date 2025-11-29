namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public enum OpCaseStateEnum
{
    Requested,
    Scheduled,
    PreOpCleared,
    OpStarted,
    RecoveryStarted,
    Discharged,
    Cancelled
}