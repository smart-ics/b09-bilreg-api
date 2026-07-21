using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record BedIgdMarkCleanCmd(
    string BedIgdId,
    string UserId)
    : IRequest<BedIgdMarkCleanResponse>, IBedIgdKey;

public record BedIgdMarkCleanResponse(string BedIgdId, string BedIgdName, string BedState);

public class BedIgdMarkCleanHandler : IRequestHandler<BedIgdMarkCleanCmd, BedIgdMarkCleanResponse>
{
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public BedIgdMarkCleanHandler(IBedIgdRepo bedIgdRepo, ITglJamProvider tglJamProvider)
    {
        _bedIgdRepo = bedIgdRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BedIgdMarkCleanResponse> Handle(BedIgdMarkCleanCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedIgdId, nameof(request.BedIgdId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);

        using var trans = TransHelper.NewScope();        
        var bed = LoadBed(request);
        bed.MarkClean(audit);
        _bedIgdRepo.SaveChanges(bed);
        trans.Complete();

        var response = new BedIgdMarkCleanResponse(bed.BedIgdId, bed.BedIgdName, bed.BedState.ToString());        

        return Task.FromResult(response);
    }

    private BedIgdModel LoadBed(BedIgdMarkCleanCmd request)
    {
        var bedMayBe = _bedIgdRepo.LoadEntity(request);

        if (!bedMayBe.HasValue)
            throw new KeyNotFoundException($"BedIgdId {request.BedIgdId} not found");

        return bedMayBe.Value;
    }
}
