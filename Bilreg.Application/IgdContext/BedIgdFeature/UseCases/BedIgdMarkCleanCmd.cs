using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record BedIgdMarkCleanCmd(
    string BedIgdId,
    string UserId)
    : IRequest<BedIgdMarkCleanResponse>, IBedIgdKey;

public record BedIgdMarkCleanResponse(string BedIgdId, string BedIgdName, string BedState);

public class BedIgdMarkCleanHandler : IRequestHandler<BedIgdMarkCleanCmd, BedIgdMarkCleanResponse>
{
    private readonly IBedIgdRepo _bedIgdRepo;

    public BedIgdMarkCleanHandler(IBedIgdRepo bedIgdRepo)
    {
        _bedIgdRepo = bedIgdRepo;
    }

    public Task<BedIgdMarkCleanResponse> Handle(BedIgdMarkCleanCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedIgdId, nameof(request.BedIgdId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var audit = new AuditInfoType(request.UserId, DateTime.Now);

        BedIgdMarkCleanResponse response;
        using (var trans = TransHelper.NewScope())
        {
            var bedMayBe = _bedIgdRepo.LoadEntity(request);
            if (!bedMayBe.HasValue)
                throw new KeyNotFoundException($"BedIgdId {request.BedIgdId} not found");
            var bed = bedMayBe.Value;

            bed.MarkClean(audit);
            _bedIgdRepo.SaveChanges(bed);
            trans.Complete();
            response = new BedIgdMarkCleanResponse(bed.BedIgdId, bed.BedIgdName, bed.BedState.ToString());
        }

        return Task.FromResult(response);
    }
}