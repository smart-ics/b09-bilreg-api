using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record IgdTransferBedCmd(
    string IgdVisitId,
    string TargetBedIgdId,
    string Reason,
    string Notes,
    string UserId)
    : IRequest<IgdTransferBedResponse>, IIgdVisitKey;

public record IgdTransferBedResponse(
    string IgdVisitId,
    string FromBedIgdId,
    string ToBedIgdId,
    string ClosedPakaiBedIgdId,
    string NewPakaiBedIgdId);

public class IgdTransferBedHandler : IRequestHandler<IgdTransferBedCmd, IgdTransferBedResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly IPakaiBedIgdRepo _pakaiBedIgdRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public IgdTransferBedHandler(
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

    public Task<IgdTransferBedResponse> Handle(IgdTransferBedCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.TargetBedIgdId, nameof(request.TargetBedIgdId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        if (!visit.HasObserved)
            throw new InvalidOperationException(
                $"Visit '{request.IgdVisitId}' tidak sedang menempati bed.");

        if (visit.IsTerminal)
            throw new InvalidOperationException(
                $"Visit '{request.IgdVisitId}' sudah terminal; transfer bed tidak diperbolehkan.");

        if (visit.BedId == request.TargetBedIgdId)
            throw new InvalidOperationException(
                $"Bed tujuan sama dengan bed saat ini ({visit.BedId}).");

        var sourceBed = _bedIgdRepo.LoadEntity(BedIgdModel.Key(visit.BedId))
            .GetValueOrThrow($"BedIgd '{visit.BedId}' not found (referenced by visit).");
        if (sourceBed.CurrentIgdVisitId != visit.IgdVisitId)
            throw new InvalidOperationException(
                $"Bed '{sourceBed.BedIgdId}' tidak ditempati oleh visit '{visit.IgdVisitId}'.");

        var targetBed = _bedIgdRepo.LoadEntity(BedIgdModel.Key(request.TargetBedIgdId))
            .GetValueOrThrow($"BedIgd '{request.TargetBedIgdId}' not found.");

        if (!targetBed.IsAvailable)
            throw new InvalidOperationException(
                $"Bed '{targetBed.BedIgdId}' tidak tersedia untuk ditempati.");

        var openPakaiBedIgd = _pakaiBedIgdRepo.LoadOpenForBed(sourceBed)
            .GetValueOrThrow($"PakaiBedIgd terbuka untuk bed '{sourceBed.BedIgdId}' tidak ditemukan.");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);

        openPakaiBedIgd.Close(audit);
        sourceBed.Release(audit);
        targetBed.Occupy(visit.IgdVisitId, audit);
        var newPakaiBedIgd = PakaiBedIgdModel.Open(visit, targetBed, audit);
        visit.TransferBed(targetBed.BedIgdId, request.Reason, request.Notes, audit);

        IgdTransferBedResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _bedIgdRepo.SaveChanges(sourceBed);
            _pakaiBedIgdRepo.SaveChanges(openPakaiBedIgd);
            _bedIgdRepo.SaveChanges(targetBed);
            _pakaiBedIgdRepo.SaveChanges(newPakaiBedIgd);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdTransferBedResponse(
                visit.IgdVisitId,
                sourceBed.BedIgdId,
                targetBed.BedIgdId,
                openPakaiBedIgd.PakaiBedIgdId,
                newPakaiBedIgd.PakaiBedIgdId);
        }

        return Task.FromResult(response);
    }
}
