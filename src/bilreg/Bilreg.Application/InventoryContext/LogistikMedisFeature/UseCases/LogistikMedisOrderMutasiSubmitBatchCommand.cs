using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisOrderMutasiSubmitBatchCommand(
    string LayananOrderId,
    string LayananTujuanId,
    string UserId,
    string Priority = "Rutin",
    string OrderNote = "-",
    IReadOnlyList<LogistikMedisOrderMutasiSubmitItemRequest>? ListItem = null
) : IRequest<LogistikMedisOrderMutasiSubmitBatchResponse>;

public record LogistikMedisOrderMutasiSubmitItemRequest(
    string BrgId,
    decimal Qty,
    string SatuanId
);

public record LogistikMedisOrderMutasiSubmitBatchResponse(string OrderMutasiId) : IOrderMutasiKey;

public class LogistikMedisOrderMutasiSubmitBatchHandler
    : IRequestHandler<LogistikMedisOrderMutasiSubmitBatchCommand, LogistikMedisOrderMutasiSubmitBatchResponse>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILogistikMedisBffDal _bffDal;

    public LogistikMedisOrderMutasiSubmitBatchHandler(
        IOrderMutasiRepo orderMutasiRepo,
        ILayananRepo layananRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        ILogistikMedisBffDal bffDal)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _layananRepo = layananRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _bffDal = bffDal;
    }

    public Task<LogistikMedisOrderMutasiSubmitBatchResponse> Handle(
        LogistikMedisOrderMutasiSubmitBatchCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId, nameof(request.LayananOrderId));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var layananOrderId = request.LayananOrderId.Trim();
        var layananTujuanId = request.LayananTujuanId.Trim();

        if (string.Equals(layananOrderId, layananTujuanId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unit layanan peminta dan unit layanan tujuan tidak boleh sama.");

        if (!Enum.TryParse<OrderMutasiPriorityEnum>(request.Priority?.Trim(), ignoreCase: true, out var priority))
            throw new ArgumentException($"Priority '{request.Priority}' tidak valid. Gunakan 'Rutin' atau 'Cito'.", nameof(request.Priority));

        var items = (request.ListItem ?? [])
            .Where(x => x is not null)
            .ToList();
        if (items.Count == 0)
            throw new ArgumentException("Permintaan stok harus memiliki minimal 1 item.", nameof(request.ListItem));

        foreach (var item in items)
        {
            Guard.Against.NullOrWhiteSpace(item.BrgId, nameof(item.BrgId));
            Guard.Against.NullOrWhiteSpace(item.SatuanId, nameof(item.SatuanId));
            if (item.Qty <= 0m)
                throw new ArgumentException($"Qty untuk barang '{item.BrgId}' harus lebih dari 0.", nameof(item.Qty));
        }

        var layananOrder = GetLayanan(layananOrderId, nameof(request.LayananOrderId));
        var layananTujuan = GetLayanan(layananTujuanId, nameof(request.LayananTujuanId));

        var stokByBrg = (_bffDal.ListStokBatch(layananOrderId) ?? [])
            .GroupBy(x => x.BrgId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QtySisa), StringComparer.OrdinalIgnoreCase);

        var orderNote = string.IsNullOrWhiteSpace(request.OrderNote) ? "-" : request.OrderNote.Trim();
        var order = OrderMutasiModel.Create(
            layananOrder: layananOrder,
            layananTujuan: layananTujuan,
            orderNote: orderNote,
            userId: request.UserId.Trim(),
            listItem: [],
            priority: priority);

        foreach (var item in items)
        {
            var brg = GetActiveBrg(item.BrgId.Trim());
            var satuan = GetSatuan(item.SatuanId.Trim());
            var stokCurrent = stokByBrg.GetValueOrDefault(brg.BrgId, 0m);

            order.AddItem(new OrderMutasiItemModel(
                noUrut: 1,
                brg: brg.ToReff(),
                qty: item.Qty,
                satuan: satuan,
                stokCurrent: stokCurrent));
        }

        order.Submit(
            layananTujuan: layananTujuan.ToReff(),
            userId: request.UserId.Trim(),
            priority: priority,
            orderNote: orderNote);

        _orderMutasiRepo.SaveChanges(order);

        return Task.FromResult(new LogistikMedisOrderMutasiSubmitBatchResponse(order.OrderMutasiId));
    }

    private LayananType GetLayanan(string layananId, string paramName)
    {
        var maybe = _layananRepo.LoadEntity(LayananType.Key(layananId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"{paramName} '{layananId}' not found.");
        return maybe.Value;
    }

    private IBrg GetActiveBrg(string brgId)
    {
        var maybe = _brgRepo.LoadEntity(BarangModel.Key(brgId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"BrgId '{brgId}' not found.");

        var brg = maybe.Value;
        if (!brg.IsAktif)
            throw new InvalidOperationException($"Barang '{brg.BrgName}' ({brg.BrgId}) berstatus Non-Aktif.");

        return brg;
    }

    private SatuanType GetSatuan(string satuanId)
    {
        var maybe = _satuanRepo.LoadEntity(SatuanType.Key(satuanId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"SatuanId '{satuanId}' not found.");
        return maybe.Value;
    }
}
