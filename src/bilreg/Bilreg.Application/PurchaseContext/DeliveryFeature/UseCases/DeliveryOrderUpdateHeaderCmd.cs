using Ardalis.GuardClauses;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryOrderUpdateHeaderCmd(
    string DeliveryOrderId,
    string SupplierId,
    string SupplierName,
    string? PoReffId,
    string Notes,
    string UserId) : IRequest, IDeliveryOrderKey;

public class DeliveryOrderUpdateHeaderHandler
    : IRequestHandler<DeliveryOrderUpdateHeaderCmd>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryOrderUpdateHeaderHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(DeliveryOrderUpdateHeaderCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId);
        Guard.Against.NullOrWhiteSpace(request.SupplierId);
        Guard.Against.NullOrWhiteSpace(request.SupplierName);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.Null(request.Notes);

        var model = Load(request);
        model.UpdateDraftHeader(
            new SupplierReff(request.SupplierId, request.SupplierName),
            request.PoReffId,
            request.Notes,
            request.UserId,
            _tglJamProvider.Now);

        using var trans = TransHelper.NewScope();
        _deliveryOrderRepo.SaveChanges(model);
        trans.Complete();
        return Task.CompletedTask;
    }

    private DeliveryOrderModel Load(IDeliveryOrderKey key)
    {
        var maybe = _deliveryOrderRepo.LoadEntity(key);
        return maybe.HasValue
            ? maybe.Value
            : throw new KeyNotFoundException(
                $"DeliveryOrderId '{key.DeliveryOrderId}' tidak ditemukan");
    }
}
