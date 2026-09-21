namespace Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

public enum SmassTaskTypeEnum
{
    Generate = 0,
    Link = 1
}

public static class SmassTaskTypeEnumExtensions
{
    public static string ToCode(this SmassTaskTypeEnum taskType) => taskType switch
    {
        SmassTaskTypeEnum.Generate => "GENERATE",
        SmassTaskTypeEnum.Link => "LINK",
        _ => "-",
    };
}
