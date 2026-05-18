using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCancelCmd(string OrderId, string UserId, string Reason)
    : IRequest, ILabOrderKey;

public class LabOrderCancelHandler : IRequestHandler<LabOrderCancelCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;

    public LabOrderCancelHandler(ILabOrderRepo labOrderRepo)
    {
        _labOrderRepo = labOrderRepo;
    }

    public Task Handle(LabOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        order.Cancel(request.UserId, request.Reason);
        _labOrderRepo.SaveChanges(order);
        return Task.CompletedTask;
    }
}
