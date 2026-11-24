using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderCancelCmd(
    string OrderId,
    string UserId,
    string Reason) : IRequest, IRadOrderKey;

public class RadOrderCancelHandler : IRequestHandler<RadOrderCancelCmd>
{
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public RadOrderCancelHandler(
        IRadOrderRepo radOrderRepo,
        ITglJamProvider tglJamProvider)
    {
        _radOrderRepo = radOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(RadOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _radOrderRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel
            );

        if (order is null)
            throw new KeyNotFoundException($"Order radiologi dengan OrderId '{request.OrderId}' tidak ditemukan.");

        order.Cancel(request.Reason, request.UserId, _tglJamProvider.Now);

        _radOrderRepo.SaveChanges(order);

        return Task.CompletedTask;
    }
}
