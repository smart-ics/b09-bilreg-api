using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record IgdAssignBedCmd(
    string IgdVisitId,
    string BedIgdId,
    string UserId)
    : IRequest<IgdAssignBedResponse>, IIgdVisitKey, IBedIgdKey;

public record IgdAssignBedResponse(string IgdVisitId, string BedIgdId, string PakaiBedId);

public class IgdAssignBedHandler : IRequestHandler<IgdAssignBedCmd, IgdAssignBedResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly IPakaiBedRepo _pakaiBedRepo;

    public IgdAssignBedHandler(
        IIgdVisitRepo igdVisitRepo,
        IBedIgdRepo bedIgdRepo,
        IPakaiBedRepo pakaiBedRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _bedIgdRepo = bedIgdRepo;
        _pakaiBedRepo = pakaiBedRepo;
    }

    public Task<IgdAssignBedResponse> Handle(IgdAssignBedCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.BedIgdId, nameof(request.BedIgdId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var bed = _bedIgdRepo.LoadEntity(request).GetValueOrThrow($"BedIgd '{request.BedIgdId}' not found");

        var audit = new AuditInfoType(request.UserId, DateTime.Now);

        bed.Occupy(visit.IgdVisitId, audit);
        visit.AssignBed(bed.BedIgdId, audit);
        var pakaiBed = PakaiBedModel.Open(visit, bed, audit);

        IgdAssignBedResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _bedIgdRepo.SaveChanges(bed);
            _pakaiBedRepo.SaveChanges(pakaiBed);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdAssignBedResponse(visit.IgdVisitId, bed.BedIgdId, pakaiBed.PakaiBedId);
        }

        return Task.FromResult(response);
    }
}
