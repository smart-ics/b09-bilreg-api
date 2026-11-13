namespace Bilreg.Domain.ApotekContext.PenjualanFeature;

public enum JenisPenjualanEnum
{
    PrescriptionBased = 1,
    RegisteredDirect = 2,
    WalkInOTC = 3
}

public enum PenjualanStatusEnum
{
    Established = 1,
    Issued = 2,
    FinanciallyCleared = 3,
    AdjustedOrCredited = 4,
    Cancelled = 5
}

public enum PayerCategoryEnum
{
    General = 1,
    Bpjs = 2,
    Insurance = 3
}
