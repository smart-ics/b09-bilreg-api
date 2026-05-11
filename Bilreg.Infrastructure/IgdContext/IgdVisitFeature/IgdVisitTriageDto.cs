using Bilreg.Domain.IgdContext.IgdVisitFeature;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public record IgdVisitTriageDto(
    string IgdVisitId,
    int NoTriage,
    string TriageLevel,
    DateTime AssessmentDateTime,
    string AssessorUserId,
    string Notes)
{
    public static IgdVisitTriageDto FromModel(string igdVisitId, IgdVisitTriageType triage)
        => new(
            IgdVisitId: igdVisitId,
            NoTriage: triage.NoTriage,
            TriageLevel: triage.Level.ToCode(),
            AssessmentDateTime: triage.AssessmentDateTime,
            AssessorUserId: triage.AssessorUserId,
            Notes: triage.Notes);

    public IgdVisitTriageType ToModel()
        => new(
            NoTriage: NoTriage,
            Level: TriageLevel.ToTriageLevelEnum(),
            AssessmentDateTime: AssessmentDateTime,
            AssessorUserId: AssessorUserId,
            Notes: Notes);
}
