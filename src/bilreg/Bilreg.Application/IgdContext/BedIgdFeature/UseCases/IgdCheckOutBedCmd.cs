using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record IgdCheckOutBedCmd(
    string IgdVisitId,
    string UserId)
    : IRequest, IIgdVisitKey;

public class IgdCheckOutBedHandler : IRequestHandler<IgdCheckOutBedCmd>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly IPakaiBedIgdRepo _pakaiBedIgdRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public IgdCheckOutBedHandler(
        IIgdVisitRepo igdVisitRepo,
        IBedIgdRepo bedIgdRepo,
        IPakaiBedIgdRepo pakaiBedIgdRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _igdVisitRepo = igdVisitRepo;
        _bedIgdRepo = bedIgdRepo;
        _pakaiBedIgdRepo = pakaiBedIgdRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(IgdCheckOutBedCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        if (!visit.HasObserved)
            throw new InvalidOperationException(
                $"Visit '{request.IgdVisitId}' tidak sedang menempati bed.");

        var bed = _bedIgdRepo.LoadEntity(BedIgdModel.Key(visit.BedId))
            .GetValueOrThrow($"BedIgd '{visit.BedId}' not found (referenced by visit).");
        if (bed.CurrentIgdVisitId != visit.IgdVisitId)
            throw new InvalidOperationException(
                $"Bed '{bed.BedIgdId}' tidak ditempati oleh visit '{visit.IgdVisitId}'.");

        var pakaiBedIgd = _pakaiBedIgdRepo.LoadOpenForBed(bed)
            .GetValueOrThrow($"PakaiBedIgd terbuka untuk bed '{bed.BedIgdId}' tidak ditemukan.");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        bed.Release(audit);
        pakaiBedIgd.Close(audit);
        visit.CheckOutBed(audit);

        using var trans = TransHelper.NewScope();
        _bedIgdRepo.SaveChanges(bed);
        _pakaiBedIgdRepo.SaveChanges(pakaiBedIgd);
        _igdVisitRepo.SaveChanges(visit);
        trans.Complete();
        return Task.CompletedTask;
    }
}
