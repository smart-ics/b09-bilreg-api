using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderDeferCmd(
    string OrderId,
    string UserId,
    string Reason,
    DateTime UntilDate)
    : IRequest, ILabOrderKey;

public class LabOrderDeferHandler : IRequestHandler<LabOrderDeferCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderDeferHandler(ILabOrderRepo labOrderRepo, ITglJamProvider? tglJamProvider = null)
    {
        _labOrderRepo = labOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabOrderDeferCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        var occurredAt = _tglJamProvider.Now;
        order.Defer(request.Reason, request.UntilDate, request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        trans.Complete();
        return Task.CompletedTask;
    }
}
