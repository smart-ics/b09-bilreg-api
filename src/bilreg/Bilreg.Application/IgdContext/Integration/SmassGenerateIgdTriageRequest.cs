namespace Bilreg.Application.IgdContext.Integration;

/// <summary>
/// Gateway request DTO for <c>POST {Smass:BaseApiUrl}/api/Assesment/generateIgdTriage</c>
/// (architecture §5.3, §6.2). It covers exactly the approved request fields; the
/// wire payload is camelCase JSON. <see cref="PaperId"/> and <see cref="LayananId"/>
/// are supplied by configuration inside the gateway (see <c>SmassAssessmentGateway</c>).
/// </summary>
public record SmassGenerateIgdTriageRequest
{
    public string IgdVisitId { get; init; } = string.Empty;

    /// <summary>Correlation key; always <c>&gt; 0</c> for a generate operation.</summary>
    public int NoTriage { get; init; }

    /// <summary>Dedicated IGD Triage Paper id (from <c>IgdVisit:SmassTriagePaperId</c>).</summary>
    public string PaperId { get; init; } = string.Empty;

    /// <summary>Configured IGD LayananId (from <c>IgdVisit:SmassLayananId</c>).</summary>
    public string LayananId { get; init; } = string.Empty;

    /// <summary>Triage operator id (stamped verbatim by SMASS, D-06).</summary>
    public string UserrId { get; init; } = string.Empty;

    /// <summary>Committed triage date, formatted <c>yyyy-MM-dd</c>.</summary>
    public string AssesmentDate { get; init; } = string.Empty;

    /// <summary>Committed triage time, formatted <c>HH:mm:ss</c>.</summary>
    public string AssesmentTime { get; init; } = string.Empty;

    public int AirwaysScore { get; init; }
    public int BreathingScore { get; init; }
    public int BloodCirculationScore { get; init; }
    public int GcsEyeScore { get; init; }
    public int GcsMotorScore { get; init; }
    public int GcsVoiceScore { get; init; }

    /// <summary>ATS level code, e.g. <c>ATS1</c>…<c>ATS5</c>.</summary>
    public string AtsLevel { get; init; } = string.Empty;

    /// <summary>Triage colour code, e.g. <c>RED</c>/<c>YELLOW</c>/<c>GREEN</c>/<c>BLACK</c>.</summary>
    public string TriageColor { get; init; } = string.Empty;

    public bool IsManualOverrideBlack { get; init; }
}
