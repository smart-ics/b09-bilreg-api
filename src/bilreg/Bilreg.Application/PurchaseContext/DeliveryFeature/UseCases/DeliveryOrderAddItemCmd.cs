using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryOrderAddItemCmd(
    string DeliveryOrderId,
    string UserId,
    DeliveryOrderDraftItemRequest Item) : IRequest, IDeliveryOrderKey;

public class DeliveryOrderAddItemHandler : IRequestHandler<DeliveryOrderAddItemCmd>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryOrderAddItemHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        ILayananRepo layananRepo,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _layananRepo = layananRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(DeliveryOrderAddItemCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.Null(request.Item);
        Guard.Against.NullOrWhiteSpace(request.Item.BrgId);
        Guard.Against.NullOrWhiteSpace(request.Item.LayananId);
        Guard.Against.NullOrWhiteSpace(request.Item.SatuanId);

        DeliveryOrderDraftReferenceValidator.Validate(
            request.Item.BrgId,
            request.Item.SatuanId,
            request.Item.LayananId,
            _brgRepo,
            _satuanRepo,
            _layananRepo);

        var model = Load(request);
        var item = DeliveryOrderItemModel.Create(
            request.Item.BrgId,
            request.Item.LayananId,
            request.Item.QtyOrder,
            request.Item.SatuanId,
            request.Item.Harga,
            request.Item.Diskon,
            request.Item.Tax,
            request.Item.TglEd,
            request.Item.NoBatch);
        model.AddItem(item, request.UserId, _tglJamProvider.Now);

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
