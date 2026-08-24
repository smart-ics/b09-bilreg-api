using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryOrderDraftItemRequest(
    string BrgId,
    string LayananId,
    decimal QtyOrder,
    string SatuanId,
    decimal Harga,
    decimal Diskon,
    decimal Tax,
    DateTime? TglEd = null,
    string? NoBatch = null);

public record DeliveryOrderCreateCmd(
    string DoNo,
    string SupplierId,
    string SupplierName,
    string? PoReffId,
    DateTime DoDate,
    string Notes,
    string UserId,
    IReadOnlyCollection<DeliveryOrderDraftItemRequest> ListItem)
    : IRequest<DeliveryOrderCreateResponse>;

public record DeliveryOrderCreateResponse(
    string DeliveryOrderId,
    DeliveryOrderStateEnum State);

public class DeliveryOrderCreateHandler
    : IRequestHandler<DeliveryOrderCreateCmd, DeliveryOrderCreateResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryOrderCreateHandler(
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

    public Task<DeliveryOrderCreateResponse> Handle(
        DeliveryOrderCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DoNo);
        Guard.Against.NullOrWhiteSpace(request.SupplierId);
        Guard.Against.NullOrWhiteSpace(request.SupplierName);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.Null(request.Notes);
        Guard.Against.Null(request.ListItem);

        if (_deliveryOrderRepo.IsDoNoExist(request.DoNo))
            throw new InvalidOperationException($"DoNo '{request.DoNo}' sudah digunakan");

        var items = request.ListItem.Select(CreateItem).ToList();
        var model = DeliveryOrderModel.Create(
            request.DoNo,
            new SupplierReff(request.SupplierId, request.SupplierName),
            request.PoReffId,
            request.DoDate,
            request.Notes,
            request.UserId,
            items,
            _tglJamProvider.Now);

        using var trans = TransHelper.NewScope();
        _deliveryOrderRepo.SaveChanges(model);
        trans.Complete();

        return Task.FromResult(new DeliveryOrderCreateResponse(model.DeliveryOrderId, model.State));
    }

    private DeliveryOrderItemModel CreateItem(DeliveryOrderDraftItemRequest item)
    {
        Guard.Against.Null(item);
        Guard.Against.NullOrWhiteSpace(item.BrgId);
        Guard.Against.NullOrWhiteSpace(item.LayananId);
        Guard.Against.NullOrWhiteSpace(item.SatuanId);

        DeliveryOrderDraftReferenceValidator.Validate(
            item.BrgId, item.SatuanId, item.LayananId,
            _brgRepo, _satuanRepo, _layananRepo);

        return DeliveryOrderItemModel.Create(
            item.BrgId,
            item.LayananId,
            item.QtyOrder,
            item.SatuanId,
            item.Harga,
            item.Diskon,
            item.Tax,
            item.TglEd,
            item.NoBatch);
    }
}
