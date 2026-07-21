using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitDischargeCmd(string IgdVisitId, string UserId)
    : IRequest<IgdVisitDischargeResponse>, IIgdVisitKey;

public record IgdVisitDischargeResponse(
    string IgdVisitId,
    string AdministrativeState,
    DateTime DischargeDateTime,
    bool BedReleased);

public class IgdVisitDischargeHandler : IRequestHandler<IgdVisitDischargeCmd, IgdVisitDischargeResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly IPakaiBedIgdRepo _pakaiBedIgdRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public IgdVisitDischargeHandler(
        IIgdVisitRepo igdVisitRepo,
        IBedIgdRepo bedIgdRepo,
        IPakaiBedIgdRepo pakaiBedIgdRepo,
        ITglJamProvider tglJamProvider)
    {
        _igdVisitRepo = igdVisitRepo;
        _bedIgdRepo = bedIgdRepo;
        _pakaiBedIgdRepo = pakaiBedIgdRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<IgdVisitDischargeResponse> Handle(IgdVisitDischargeCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        if (visit.IsDischarged)
        {
            return Task.FromResult(new IgdVisitDischargeResponse(
                visit.IgdVisitId,
                visit.AdministrativeState.ToCode(),
                visit.DischargeAudit.Timestamp,
                BedReleased: false));
        }

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);

        BedIgdModel? bed = null;
        PakaiBedIgdModel? pakaiBedIgd = null;
        var bedReleased = false;

        if (visit.HasObserved)
        {
            bed = _bedIgdRepo.LoadEntity(BedIgdModel.Key(visit.BedId))
                .GetValueOrThrow($"BedIgd '{visit.BedId}' not found (referenced by visit).");
            if (bed.CurrentIgdVisitId != visit.IgdVisitId)
                throw new InvalidOperationException(
                    $"Bed '{bed.BedIgdId}' tidak ditempati oleh visit '{visit.IgdVisitId}'.");

            pakaiBedIgd = _pakaiBedIgdRepo.LoadOpenForBed(bed)
                .GetValueOrThrow($"PakaiBedIgd terbuka untuk bed '{bed.BedIgdId}' tidak ditemukan.");

            bed.Release(audit);
            pakaiBedIgd.Close(audit);
            visit.ClearBed(audit);
            bedReleased = true;
        }

        visit.Discharge(audit);

        using (var trans = TransHelper.NewScope())
        {
            if (bed is not null) _bedIgdRepo.SaveChanges(bed);
            if (pakaiBedIgd is not null) _pakaiBedIgdRepo.SaveChanges(pakaiBedIgd);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
        }

        return Task.FromResult(new IgdVisitDischargeResponse(
            visit.IgdVisitId,
            visit.AdministrativeState.ToCode(),
            visit.DischargeAudit.Timestamp,
            bedReleased));
    }
}
