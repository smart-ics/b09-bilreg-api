namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public enum TriageLevelEnum
{
    Unknown = 0,
    P1Resusitasi = 1,
    P2Emergent = 2,
    P3Urgent = 3,
    P4NonUrgent = 4,
    P5False = 5,
}

public static class TriageLevelEnumExtensions
{
    public static string ToCode(this TriageLevelEnum level) => level switch
    {
        TriageLevelEnum.P1Resusitasi => "P1",
        TriageLevelEnum.P2Emergent => "P2",
        TriageLevelEnum.P3Urgent => "P3",
        TriageLevelEnum.P4NonUrgent => "P4",
        TriageLevelEnum.P5False => "P5",
        _ => "-",
    };

    public static TriageLevelEnum ToTriageLevelEnum(this string code) => code switch
    {
        "P1" => TriageLevelEnum.P1Resusitasi,
        "P2" => TriageLevelEnum.P2Emergent,
        "P3" => TriageLevelEnum.P3Urgent,
        "P4" => TriageLevelEnum.P4NonUrgent,
        "P5" => TriageLevelEnum.P5False,
        _ => TriageLevelEnum.Unknown,
    };
}

public record IgdVisitTriageType(
    int NoTriage,
    TriageLevelEnum Level,
    DateTime AssessmentDateTime,
    string AssessorUserId,
    string Notes)
{
    public static IgdVisitTriageType Default => new(
        NoTriage: 0,
        Level: TriageLevelEnum.Unknown,
        AssessmentDateTime: new DateTime(3000, 1, 1),
        AssessorUserId: "-",
        Notes: "-");
}
