using Ardalis.GuardClauses;
using Bilreg.Application.RadContext.RadOrderFeature.Integration;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderChargeCmd(string OrderId, string UserId) : IRequest<RadOrderChargeResponse>, IRadOrderKey;

public record RadOrderChargeResponse(bool Success, string? BillingTindakanId, string? BillingLastError);

public class RadOrderChargeHandler : IRequestHandler<RadOrderChargeCmd, RadOrderChargeResponse>
{
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly IRadBillingIntegration _radBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;

    public RadOrderChargeHandler(
        IRadOrderRepo radOrderRepo,
        IRadBillingIntegration radBillingIntegration,
        ITglJamProvider tglJamProvider)
    {
        _radOrderRepo = radOrderRepo;
        _radBillingIntegration = radBillingIntegration;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RadOrderChargeResponse> Handle(RadOrderChargeCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _radOrderRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel
            );

        if (order is null)
            throw new KeyNotFoundException($"Order radiologi '{request.OrderId}' tidak ditemukan.");

        var occurredAt = _tglJamProvider.Now;
        order.Charge(request.UserId);

        var success = false;
        try
        {
            var billingRefId = _radBillingIntegration.ChargeOrder(order, request.UserId, occurredAt);
            order.MarkCharged(billingRefId, request.UserId, occurredAt);
            success = true;
        }
        catch (RadBillingChargeException ex)
        {
            order.RecordBillingError(ex.Message, request.UserId, occurredAt);
        }

        using (var trans = TransHelper.NewScope())
        {
            _radOrderRepo.SaveChanges(order);
            trans.Complete();
        }

        var response = new RadOrderChargeResponse(
            success,
            string.IsNullOrWhiteSpace(order.BillingTindakanId) ? null : order.BillingTindakanId,
            string.IsNullOrWhiteSpace(order.BillingLastError) ? null : order.BillingLastError);

        return Task.FromResult(response);
    }
}
