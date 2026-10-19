using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public record PakaiBedReleaseCommand(
    string PakaiBedId,
    DateTime TglJamOut,
    string UserId,
    string? CheckoutReason = null
) : IRequest<PakaiBedReleaseResult>;

public record PakaiBedReleaseResult(
    string PakaiBedId,
    DateTime TglJamOut,
    RoomChargeReconcileResponse? ReconcileResult = null
);

public class PakaiBedReleaseCommandHandler : IRequestHandler<PakaiBedReleaseCommand, PakaiBedReleaseResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public PakaiBedReleaseCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<PakaiBedReleaseResult> Handle(PakaiBedReleaseCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        var pakaiBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
            .GetValueOrThrow($"Pakai Bed '{request.PakaiBedId}' not found");

        var occurredAt = request.TglJamOut == default ? _tglJamProvider.Now : request.TglJamOut;

        pakaiBed.Release(request.UserId, occurredAt);
        _pakaiBedRepo.SaveChanges(pakaiBed);

        // Checkout hook: trigger/send RoomChargeReconcileCheckoutCmd via _mediator
        var reconcileCmd = new RoomChargeReconcileCheckoutCmd(request.PakaiBedId, occurredAt, request.UserId);
        var reconcileResult = await _mediator.Send(reconcileCmd, cancellationToken);

        return new PakaiBedReleaseResult(request.PakaiBedId, occurredAt, reconcileResult);
    }
}
