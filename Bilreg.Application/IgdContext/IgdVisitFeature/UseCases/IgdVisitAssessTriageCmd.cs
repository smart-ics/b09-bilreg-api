using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitAssessTriageCmd(
    string IgdVisitId,
    string TriageLevel,
    string Notes,
    string UserId)
    : IRequest<IgdVisitAssessTriageResponse>, IIgdVisitKey;

public record IgdVisitAssessTriageResponse(string IgdVisitId, int NoTriage, string TriageLevel);

public class IgdVisitAssessTriageHandler : IRequestHandler<IgdVisitAssessTriageCmd, IgdVisitAssessTriageResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;

    public IgdVisitAssessTriageHandler(IIgdVisitRepo igdVisitRepo)
    {
        _igdVisitRepo = igdVisitRepo;
    }

    public Task<IgdVisitAssessTriageResponse> Handle(IgdVisitAssessTriageCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.TriageLevel, nameof(request.TriageLevel));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var level = request.TriageLevel.ToTriageLevelEnum();
        if (level == TriageLevelEnum.Unknown)
            throw new ArgumentException($"TriageLevel '{request.TriageLevel}' tidak dikenal (gunakan P1..P5).");

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        visit.AssessTriage(level, request.Notes ?? "-", audit);

        using var trans = TransHelper.NewScope();
        _igdVisitRepo.SaveChanges(visit);
        trans.Complete();

        return Task.FromResult(new IgdVisitAssessTriageResponse(
            visit.IgdVisitId, visit.Triage.NoTriage, visit.Triage.Level.ToCode()));
    }
}
