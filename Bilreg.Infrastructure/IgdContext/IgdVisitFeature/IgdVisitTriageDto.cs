using Bilreg.Domain.IgdContext.IgdVisitFeature;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public record IgdVisitTriageDto(
    string IgdVisitId,
    int NoTriage,
    string TriageMethod,
    string TriageLevel,
    string TriageColor,
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
    public static IgdVisitTriageDto FromModel(string igdVisitId, IgdVisitTriageType triage)
        => new(
            IgdVisitId: igdVisitId,
            NoTriage: triage.NoTriage,
            TriageMethod: triage.Method.ToCode(),
            TriageLevel: triage.Level.ToCode(),
            TriageColor: triage.Color.ToCode(),
            AirwaysScore: triage.AirwaysScore,
            BreathingScore: triage.BreathingScore,
            BloodCirculationScore: triage.BloodCirculationScore,
            GcsEyeScore: triage.GcsEyeScore,
            GcsMotorScore: triage.GcsMotorScore,
            GcsVoiceScore: triage.GcsVoiceScore,
            IsManualOverrideBlack: triage.IsManualOverrideBlack,
            OverrideByUserId: triage.OverrideByUserId,
            OverrideReason: triage.OverrideReason,
            OverrideDateTime: triage.OverrideDateTime,
            AssessmentDateTime: triage.AssessmentDateTime,
            AssessorUserId: triage.AssessorUserId,
            Notes: triage.Notes);

    public IgdVisitTriageType ToModel()
        => new(
            NoTriage: NoTriage,
            Method: TriageMethod.ToTriageMethodEnum(),
            Level: TriageLevel.ToTriageLevelEnum(),
            Color: TriageColor.ToTriageColorEnum(),
            AirwaysScore: AirwaysScore,
            BreathingScore: BreathingScore,
            BloodCirculationScore: BloodCirculationScore,
            GcsEyeScore: GcsEyeScore,
            GcsMotorScore: GcsMotorScore,
            GcsVoiceScore: GcsVoiceScore,
            IsManualOverrideBlack: IsManualOverrideBlack,
            OverrideByUserId: OverrideByUserId,
            OverrideReason: OverrideReason,
            OverrideDateTime: OverrideDateTime,
            AssessmentDateTime: AssessmentDateTime,
            AssessorUserId: AssessorUserId,
            Notes: Notes);
}
