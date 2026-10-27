using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiAddItemCommand(
    string BrgId,
    decimal Qty,
    string SatuanId,
    string LayananOrderId,
    string UserId,
    string OrderMutasiId = "") : IRequest, IBrgKey, ISatuanKey
{
    public OrderMutasiAddItemCommand(
        string orderMutasiId,
        string layananOrderId,
        string brgId,
        decimal qty,
        string satuanId,
        string userId = "")
        : this(brgId, qty, satuanId, layananOrderId, userId, orderMutasiId)
    {
    }
}

public class OrderMutasiAddItemHandler : IRequestHandler<OrderMutasiAddItemCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IStokLokasiDal? _stokLokasiDal;

    public OrderMutasiAddItemHandler(
        IOrderMutasiRepo orderMutasiRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        ILayananRepo layananRepo,
        IStokLokasiDal? stokLokasiDal = null)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _layananRepo = layananRepo;
        _stokLokasiDal = stokLokasiDal;
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
        var stokCurrent = _stokLokasiDal?.ListAllocationCandidates(request.BrgId, request.LayananOrderId)?.Sum(x => x.QtySisa) ?? 0m;
        var item = CreateItem(request, stokCurrent);
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

        var brg = mayBe.Value;
        if (!brg.IsAktif)
            throw new InvalidOperationException($"Barang '{brg.BrgName}' ({brg.BrgId}) berstatus Non-Aktif.");

        return brg.ToReff();
    }

    private OrderMutasiModel GetOrderDraft(OrderMutasiAddItemCommand request)
    {
        if (!string.IsNullOrWhiteSpace(request.OrderMutasiId))
        {
            var requestedKey = OrderMutasiModel.Key(request.OrderMutasiId);
            var requestedMaybe = _orderMutasiRepo.LoadEntity(requestedKey);
            if (requestedMaybe.HasValue)
                return requestedMaybe.Value;
        }

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

    private OrderMutasiItemModel CreateItem(OrderMutasiAddItemCommand request, decimal stokCurrent)
    {
        var brg = GetBrg(request);
        var satuan = GetSatuan(request);
        var item = new OrderMutasiItemModel(1, brg, request.Qty, satuan, stokCurrent);

        return item;
    }
    #endregion
}
