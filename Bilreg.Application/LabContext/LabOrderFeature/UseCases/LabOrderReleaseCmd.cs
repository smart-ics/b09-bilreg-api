using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderReleaseCmd(string OrderId, string UserId, string ReleaseNote)
    : IRequest, ILabOrderKey;

public class LabOrderReleaseHandler : IRequestHandler<LabOrderReleaseCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;

    public LabOrderReleaseHandler(ILabOrderRepo labOrderRepo)
    {
        _labOrderRepo = labOrderRepo;
    }

    public Task Handle(LabOrderReleaseCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.ReleaseNote, nameof(request.ReleaseNote));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        order.Release(request.UserId, request.ReleaseNote);
        _labOrderRepo.SaveChanges(order);
        return Task.CompletedTask;
    }
}
