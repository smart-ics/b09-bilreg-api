using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using MediatR;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitGetQuery(string IgdVisitId) : IRequest<IgdVisitGetResponse>, IIgdVisitKey;

public record IgdVisitGetResponse(
    string IgdVisitId,
    DateTime DaftarDateTime,
    string VisitorName,
    string VisitorGender,
    DateOnly VisitorTglLahir,
    string VisitorKontak,
    string DokterId,
    string DokterName,
    bool HasTriage,
    string TriageMethod,
    string TriageLevel,
    string TriageColor,
    DateTime LastTriageAt,
    DateTime NextReTriageAt,
    string AdministrativeState,
    string RegId,
    string PasienId,
    string PasienName,
    string RedirectRajalId,
    string BedIgdId,
    bool IsVoided,
    IEnumerable<TriageItem> ListTriage,
    IEnumerable<EventItem> ListEvent);

public record TriageItem(
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
    string Notes);
public record EventItem(int NoEvent, string EventKind, DateTime EventDateTime, string UserId, string Notes);

public class IgdVisitGetHandler : IRequestHandler<IgdVisitGetQuery, IgdVisitGetResponse>
{
    private readonly IIgdVisitRepo _repo;

    public IgdVisitGetHandler(IIgdVisitRepo repo)
    {
        _repo = repo;
    }

    public Task<IgdVisitGetResponse> Handle(IgdVisitGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));

        var visit = _repo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var listTriage = visit.ListTriage.Select(x =>
            new TriageItem(
                x.NoTriage, x.Method.ToCode(), x.Level.ToCode(), x.Color.ToCode(),
                x.AirwaysScore, x.BreathingScore, x.BloodCirculationScore,
                x.GcsEyeScore, x.GcsMotorScore, x.GcsVoiceScore,
                x.IsManualOverrideBlack, x.OverrideByUserId, x.OverrideReason, x.OverrideDateTime,
                x.AssessmentDateTime, x.AssessorUserId, x.Notes));
        var listEvent = visit.ListEvent.Select(x =>
            new EventItem(x.NoEvent, x.EventKind.ToCode(), x.EventDateTime, x.UserId, x.Notes));

        var response = new IgdVisitGetResponse(
            IgdVisitId: visit.IgdVisitId,
            DaftarDateTime: visit.DaftarDateTime,
            VisitorName: visit.Visitor.VisitorName,
            VisitorGender: visit.Visitor.Gender,
            VisitorTglLahir: visit.Visitor.TglLahir,
            VisitorKontak: visit.Visitor.Kontak,
            DokterId: visit.Dokter.PpaId,
            DokterName: visit.Dokter.PpaName,
            HasTriage: visit.HasTriage,
            TriageMethod: visit.HasTriage ? visit.TriageMethod.ToCode() : "",
            TriageLevel: visit.HasTriage ? visit.Triage.Level.ToCode() : "",
            TriageColor: visit.HasTriage ? visit.TriageColor.ToCode() : "",
            LastTriageAt: visit.LastTriageAt,
            NextReTriageAt: visit.NextReTriageAt,
            AdministrativeState: visit.AdministrativeState.ToCode(),
            RegId: visit.Reg.RegId,
            PasienId: visit.Reg.PasienId,
            PasienName: visit.Reg.PasienName,
            RedirectRajalId: visit.Redirection.RedirectRajalId,
            BedIgdId: visit.BedId,
            IsVoided: visit.IsVoided,
            ListTriage: listTriage.ToList(),
            ListEvent: listEvent.ToList());

        return Task.FromResult(response);
    }
}
