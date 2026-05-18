using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderChargeCmd(string OrderId, string UserId)
    : IRequest<LabOrderChargeResponse>, ILabOrderKey;

public record LabOrderChargeResponse(
    bool Success,
    string? BillingTindakanId,
    string? BillingLastError);

public class LabOrderChargeHandler : IRequestHandler<LabOrderChargeCmd, LabOrderChargeResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabBillingIntegration _labBillingIntegration;

    public LabOrderChargeHandler(ILabOrderRepo labOrderRepo, ILabBillingIntegration labBillingIntegration)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
    }

    public Task<LabOrderChargeResponse> Handle(LabOrderChargeCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        order.Charge(request.UserId);

        var success = false;
        try
        {
            var tindakanId = _labBillingIntegration.CreateTindakan(
                new LabBillingChargeRequest(request.OrderId, request.UserId));
            order.MarkCharged(tindakanId, request.UserId);
            success = true;
        }
        catch (LabBillingChargeException ex)
        {
            order.RecordBillingError(ex.Message, request.UserId);
        }

        LabOrderChargeResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderChargeResponse(
                success,
                string.IsNullOrWhiteSpace(order.BillingTindakanId) ? null : order.BillingTindakanId,
                string.IsNullOrWhiteSpace(order.BillingLastError) ? null : order.BillingLastError);
        }

        return Task.FromResult(response);
    }
}
