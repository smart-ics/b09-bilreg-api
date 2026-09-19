namespace Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

public enum SmassTaskStatusEnum
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}

public static class SmassTaskStatusEnumExtensions
{
    public static string ToCode(this SmassTaskStatusEnum taskStatus) => taskStatus switch
    {
        SmassTaskStatusEnum.Pending => "PENDING",
        SmassTaskStatusEnum.Succeeded => "SUCCEEDED",
        SmassTaskStatusEnum.Failed => "FAILED",
        _ => "-",
    };
}
