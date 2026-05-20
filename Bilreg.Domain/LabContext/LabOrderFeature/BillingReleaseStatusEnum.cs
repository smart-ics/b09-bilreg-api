namespace Bilreg.Domain.LabContext.LabOrderFeature;

public enum BillingReleaseStatusEnum
{
    Clear = 1,
    Blocked = 2
}

public static class BillingReleaseStatusApi
{
    public const string Clear = "CLEAR";
    public const string Blocked = "BLOCKED";

    public static string ToApi(BillingReleaseStatusEnum status) =>
        status == BillingReleaseStatusEnum.Clear ? Clear : Blocked;
}
