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
    string TriageLevel,
    string AdministrativeState,
    string RegId,
    string PasienId,
    string PasienName,
    string RedirectRajalId,
    string BedIgdId,
    bool IsVoided,
    IEnumerable<TriageItem> ListTriage,
    IEnumerable<EventItem> ListEvent);

public record TriageItem(int NoTriage, string TriageLevel, DateTime AssessmentDateTime, string AssessorUserId, string Notes);
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
            new TriageItem(x.NoTriage, x.Level.ToCode(), x.AssessmentDateTime, x.AssessorUserId, x.Notes));
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
            TriageLevel: visit.HasTriage ? visit.Triage.Level.ToCode() : "",
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
