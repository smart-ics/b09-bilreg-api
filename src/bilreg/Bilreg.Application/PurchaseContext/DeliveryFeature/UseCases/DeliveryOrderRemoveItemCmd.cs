using Ardalis.GuardClauses;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryOrderRemoveItemCmd(
    string DeliveryOrderId,
    int ItemNo,
    string UserId) : IRequest, IDeliveryOrderKey;

public class DeliveryOrderRemoveItemHandler : IRequestHandler<DeliveryOrderRemoveItemCmd>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryOrderRemoveItemHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(DeliveryOrderRemoveItemCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId);
        Guard.Against.NegativeOrZero(request.ItemNo);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var maybe = _deliveryOrderRepo.LoadEntity(request);
        if (!maybe.HasValue)
            throw new KeyNotFoundException(
                $"DeliveryOrderId '{request.DeliveryOrderId}' tidak ditemukan");

        var model = maybe.Value;
        model.RemoveItem(request.ItemNo, request.UserId, _tglJamProvider.Now);

        using var trans = TransHelper.NewScope();
        _deliveryOrderRepo.SaveChanges(model);
        trans.Complete();
        return Task.CompletedTask;
    }
}
