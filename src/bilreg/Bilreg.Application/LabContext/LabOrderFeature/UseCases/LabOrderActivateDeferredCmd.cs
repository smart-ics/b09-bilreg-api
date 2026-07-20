using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderActivateDeferredCmd(string OrderId, string UserId)
    : IRequest<LabOrderActivateDeferredResponse>, ILabOrderKey;

public record LabOrderActivateDeferredResponse(string ExecutionRegId);

public class LabOrderActivateDeferredHandler
    : IRequestHandler<LabOrderActivateDeferredCmd, LabOrderActivateDeferredResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabRegIntegration _labRegIntegration;
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderActivateDeferredHandler(ILabOrderRepo labOrderRepo, ILabRegIntegration labRegIntegration,
        ITglJamProvider? tglJamProvider = null)
    {
        _labOrderRepo = labOrderRepo;
        _labRegIntegration = labRegIntegration;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabOrderActivateDeferredResponse> Handle(
        LabOrderActivateDeferredCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        var executionRegId = _labRegIntegration.CreateExecutionRegistration(
            new LabRegExecutionRequest(request.OrderId, request.UserId));
        var occurredAt = _tglJamProvider.Now;
        order.ActivateFromDeferred(executionRegId, request.UserId, occurredAt);

        LabOrderActivateDeferredResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderActivateDeferredResponse(executionRegId);
        }

        return Task.FromResult(response);
    }
}
