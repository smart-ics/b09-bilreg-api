using Ardalis.GuardClauses;
using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Application.InventoryContext.StokFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiAddItemCommand(string BrgId, int Qty, string SatuanId, 
    string LayananOrderId, string UserId) : IRequest, IBrgKey, ISatuanKey;

public class OrderMutasiAddItemHandler : IRequestHandler<OrderMutasiAddItemCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILayananRepo _layananRepo;

    public OrderMutasiAddItemHandler(IOrderMutasiRepo orderMutasiRepo, 
        IBrgRepo brgRepo, ISatuanRepo satuanRepo, ILayananRepo layananRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _layananRepo = layananRepo;
    }

    public Task Handle(OrderMutasiAddItemCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.SatuanId);
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId);
        Guard.Against.NegativeOrZero(request.Qty);

        // BUILD
        var order = GetOrderDraft(request);        
        var item = CreateItem(request);        
        order.AddItem(item);

        // WRITE
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    #region PRIVATE-HELPERS
    private SatuanType GetSatuan(OrderMutasiAddItemCommand request)
    {
        var mayBe = _satuanRepo.LoadEntity(request);
        if (!mayBe.HasValue)
            throw new KeyNotFoundException($"SatuanId {request.SatuanId} not found");
        return mayBe.Value;
    }

    private BrgReff GetBrg(OrderMutasiAddItemCommand request)
    {
        var mayBe = _brgRepo.LoadEntity(request);
        if (!mayBe.HasValue)
            throw new KeyNotFoundException($"BrgId {request.BrgId} not found");
        return mayBe.Value.ToReff();
    }

    private OrderMutasiModel GetOrderDraft(OrderMutasiAddItemCommand request)
    {
        var header = _orderMutasiRepo.ListDraftState()
            .FirstOrDefault(x => x.LayananOrder.LayananId == request.LayananOrderId);
        if (header is null)
            return CreateOrderDraft(request);

        var key = OrderMutasiModel.Key(header.OrderMutasiId);
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new ArgumentException($"OrderMutasiId {header.OrderMutasiId} not found");

        return maybe.Value;
    }

    private OrderMutasiModel CreateOrderDraft(OrderMutasiAddItemCommand request)
    {
        var key = LayananType.Key(request.LayananOrderId);
        var layanan = _layananRepo.LoadEntity(key);
        if (!layanan.HasValue)
            throw new KeyNotFoundException($"LayananOrderId {key.LayananId} not found");

        return OrderMutasiModel.Create(
            layananOrder: layanan.Value,
            layananTujuan: LayananType.Default,
            orderNote: "-",
            userId: request.UserId,
            listItem: []
        );
    }

    private OrderMutasiItemModel CreateItem(OrderMutasiAddItemCommand request)
    {
        var brg = GetBrg(request);
        var satuan = GetSatuan(request);
        var item = new OrderMutasiItemModel(1, brg, request.Qty, satuan);

        return item;
    }
    #endregion
}