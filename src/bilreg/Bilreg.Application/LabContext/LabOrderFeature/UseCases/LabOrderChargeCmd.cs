using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;
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
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderChargeHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabOrderChargeResponse> Handle(LabOrderChargeCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        var occurredAt = _tglJamProvider.Now;
        order.Charge(request.UserId);

        var success = false;
        try
        {
            var billingRefId = _labBillingIntegration.ChargeOrder(order, request.UserId, occurredAt);
            order.MarkCharged(billingRefId, request.UserId, occurredAt);
            success = true;
        }
        catch (LabBillingChargeException ex)
        {
            order.RecordBillingError(ex.Message, request.UserId, occurredAt);
        }

        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
        }

        var response = new LabOrderChargeResponse(
            success,
            string.IsNullOrWhiteSpace(order.BillingTindakanId) ? null : order.BillingTindakanId,
            string.IsNullOrWhiteSpace(order.BillingLastError) ? null : order.BillingLastError);

        return Task.FromResult(response);
    }
}