using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderApproveFinancialClearanceCmd(string OrderId, string UserId)
    : IRequest, ILabOrderKey;

public class LabOrderApproveFinancialClearanceHandler : IRequestHandler<LabOrderApproveFinancialClearanceCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;

    public LabOrderApproveFinancialClearanceHandler(ILabOrderRepo labOrderRepo)
    {
        _labOrderRepo = labOrderRepo;
    }

    public Task Handle(LabOrderApproveFinancialClearanceCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        order.ApproveFinancialClearance(request.UserId);
        _labOrderRepo.SaveChanges(order);
        return Task.CompletedTask;
    }
}
