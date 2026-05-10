using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitAssignDokterCmd(
    string IgdVisitId,
    string DokterId,
    string UserId)
    : IRequest, IIgdVisitKey, IPpaKey
{
    public string PpaId => DokterId;
}

public class IgdVisitAssignDokterHandler : IRequestHandler<IgdVisitAssignDokterCmd>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IPpaRepo _ppaRepo;

    public IgdVisitAssignDokterHandler(IIgdVisitRepo igdVisitRepo, IPpaRepo ppaRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _ppaRepo = ppaRepo;
    }

    public Task Handle(IgdVisitAssignDokterCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var dokter = _ppaRepo.LoadEntity(request).GetValueOrThrow($"Dokter '{request.DokterId}' not found");

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        visit.AssignDokter(dokter, audit);

        using var trans = TransHelper.NewScope();
        _igdVisitRepo.SaveChanges(visit);
        trans.Complete();
        return Task.CompletedTask;
    }
}
