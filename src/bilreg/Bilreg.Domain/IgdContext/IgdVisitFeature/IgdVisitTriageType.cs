namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public enum TriageMethodEnum
{
    Unknown = 0,
    Ats = 1,
    Esi = 2,
    Ctas = 3,
    Mts = 4,
}

public static class TriageMethodEnumExtensions
{
    public static string ToCode(this TriageMethodEnum method) => method switch
    {
        TriageMethodEnum.Ats => "ATS",
        TriageMethodEnum.Esi => "ESI",
        TriageMethodEnum.Ctas => "CTAS",
        TriageMethodEnum.Mts => "MTS",
        _ => "-",
    };

    public static TriageMethodEnum ToTriageMethodEnum(this string code) => code switch
    {
        "ATS" => TriageMethodEnum.Ats,
        "ESI" => TriageMethodEnum.Esi,
        "CTAS" => TriageMethodEnum.Ctas,
        "MTS" => TriageMethodEnum.Mts,
        _ => TriageMethodEnum.Unknown,
    };
}

public enum TriageLevelEnum
{
    Unknown = 0,
    Ats1 = 1,
    Ats2 = 2,
    Ats3 = 3,
    Ats4 = 4,
    Ats5 = 5,
}

public static class TriageLevelEnumExtensions
{
    public static string ToCode(this TriageLevelEnum level) => level switch
    {
        TriageLevelEnum.Ats1 => "ATS1",
        TriageLevelEnum.Ats2 => "ATS2",
        TriageLevelEnum.Ats3 => "ATS3",
        TriageLevelEnum.Ats4 => "ATS4",
        TriageLevelEnum.Ats5 => "ATS5",
        _ => "-",
    };

    public static TriageLevelEnum ToTriageLevelEnum(this string code) => code switch
    {
        "ATS1" or "P1" => TriageLevelEnum.Ats1,
        "ATS2" or "P2" => TriageLevelEnum.Ats2,
        "ATS3" or "P3" => TriageLevelEnum.Ats3,
        "ATS4" or "P4" => TriageLevelEnum.Ats4,
        "ATS5" or "P5" => TriageLevelEnum.Ats5,
        _ => TriageLevelEnum.Unknown,
    };

    public static TimeSpan? ToReAssessmentInterval(this TriageLevelEnum level) => level switch
    {
        TriageLevelEnum.Ats1 => null, // continuous monitoring
        TriageLevelEnum.Ats2 => TimeSpan.FromMinutes(15),
        TriageLevelEnum.Ats3 => TimeSpan.FromMinutes(30),
        TriageLevelEnum.Ats4 => TimeSpan.FromMinutes(60),
        TriageLevelEnum.Ats5 => TimeSpan.FromMinutes(120),
        _ => null,
    };
}

public enum TriageColorEnum
{
    Unknown = 0,
    Red = 1,
    Yellow = 2,
    Green = 3,
    Black = 4,
}

public static class TriageColorEnumExtensions
{
    public static string ToCode(this TriageColorEnum color) => color switch
    {
        TriageColorEnum.Red => "RED",
        TriageColorEnum.Yellow => "YELLOW",
        TriageColorEnum.Green => "GREEN",
        TriageColorEnum.Black => "BLACK",
        _ => "-",
    };

    public static TriageColorEnum ToTriageColorEnum(this string code) => code switch
    {
        "RED" => TriageColorEnum.Red,
        "YELLOW" => TriageColorEnum.Yellow,
        "GREEN" => TriageColorEnum.Green,
        "BLACK" => TriageColorEnum.Black,
        _ => TriageColorEnum.Unknown,
    };
}

public record IgdVisitTriageType(
    int NoTriage,
    TriageMethodEnum Method,
    TriageLevelEnum Level,
    TriageColorEnum Color,
    int AirwaysScore,
    int BreathingScore,
    int BloodCirculationScore,
    int GcsEyeScore,
    int GcsMotorScore,
    int GcsVoiceScore,
    bool IsManualOverrideBlack,
    string OverrideByUserId,
    string OverrideReason,
    DateTime OverrideDateTime,
    DateTime AssessmentDateTime,
    string AssessorUserId,
    string Notes)
{
    public static IgdVisitTriageType Default => new(
        NoTriage: 0,
        Method: TriageMethodEnum.Unknown,
        Level: TriageLevelEnum.Unknown,
        Color: TriageColorEnum.Unknown,
        AirwaysScore: 0,
        BreathingScore: 0,
        BloodCirculationScore: 0,
        GcsEyeScore: 0,
        GcsMotorScore: 0,
        GcsVoiceScore: 0,
        IsManualOverrideBlack: false,
        OverrideByUserId: "-",
        OverrideReason: "-",
        OverrideDateTime: new DateTime(3000, 1, 1),
        AssessmentDateTime: new DateTime(3000, 1, 1),
        AssessorUserId: "-",
        Notes: "-");
}

public record AtsAssessmentType(
    int AirwaysScore,
    int BreathingScore,
    int BloodCirculationScore,
    int GcsEyeScore,
    int GcsMotorScore,
    int GcsVoiceScore);
