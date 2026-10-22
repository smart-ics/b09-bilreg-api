using Ardalis.GuardClauses;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCancelCmd(string OrderId, string UserId, string Reason)
    : IRequest, ILabOrderKey;

public class LabOrderCancelHandler : IRequestHandler<LabOrderCancelCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IBillingLockValidationService _billingLockValidator;

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo, 
        ITglJamProvider tglJamProvider,
        IBillingLockValidationService billingLockValidator)
    {
        _labOrderRepo = labOrderRepo;
        _tglJamProvider = tglJamProvider;
        _billingLockValidator = billingLockValidator;
    }

    public async Task Handle(LabOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        await _billingLockValidator.EnsureCanModifyBillingAsync(order.ExecutionRegId, cancellationToken);

        var occurredAt = _tglJamProvider.Now;
        order.Cancel(request.UserId, request.Reason, occurredAt);
        _labOrderRepo.SaveChanges(order);
    }
}
