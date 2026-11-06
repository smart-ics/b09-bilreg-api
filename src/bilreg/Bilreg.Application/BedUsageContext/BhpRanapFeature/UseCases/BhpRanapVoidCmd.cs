using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;

public record BhpRanapVoidCmd(
    string BhpRanapId,
    string UserId,
    string VoidReason)
    : IRequest<BhpRanapVoidResponse>, IBhpRanapKey;

public record BhpRanapVoidResponse(string BhpRanapId, bool Success);

public class BhpRanapVoidHandler : IRequestHandler<BhpRanapVoidCmd, BhpRanapVoidResponse>
{
    private readonly IBhpRanapRepo _bhpRanapRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public BhpRanapVoidHandler(
        IBhpRanapRepo bhpRanapRepo,
        ITglJamProvider tglJamProvider)
    {
        _bhpRanapRepo = bhpRanapRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BhpRanapVoidResponse> Handle(BhpRanapVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BhpRanapId, nameof(request.BhpRanapId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var maybeBhp = _bhpRanapRepo.LoadEntity(BhpRanapModel.Key(request.BhpRanapId));
        if (!maybeBhp.HasValue)
        {
            throw new KeyNotFoundException($"BhpRanap '{request.BhpRanapId}' not found.");
        }

        var bhp = maybeBhp.Value;
        if (bhp.IsVoid)
        {
            throw new InvalidOperationException($"BhpRanap '{request.BhpRanapId}' sudah dibatalkan sebelumnya.");
        }

        bhp.Void(request.UserId, _tglJamProvider.Now, request.VoidReason);

        using (var trans = TransHelper.NewScope())
        {
            _bhpRanapRepo.SaveChanges(bhp);
            trans.Complete();
        }

        return Task.FromResult(new BhpRanapVoidResponse(bhp.BhpRanapId, true));
    }
}
