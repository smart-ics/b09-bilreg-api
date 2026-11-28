using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisMutasiKeluarDispatchBatchCommand(
    string LayananAsalId,
    string LayananTujuanId,
    string UserId,
    string DispatchNote = "",
    string OrderMutasiId = "",
    IReadOnlyList<LogistikMedisMutasiKeluarDispatchItemRequest>? ListItem = null
) : IRequest<LogistikMedisMutasiKeluarDispatchBatchResponse>;

public record LogistikMedisMutasiKeluarDispatchItemRequest(
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId
);

public record LogistikMedisMutasiKeluarDispatchBatchResponse(string MutasiBrgId) : IMutasiBrgKey;

public class LogistikMedisMutasiKeluarDispatchBatchHandler
    : IRequestHandler<LogistikMedisMutasiKeluarDispatchBatchCommand, LogistikMedisMutasiKeluarDispatchBatchResponse>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly IMutasiBrgMovementService _movementService;
    private readonly ISqlDatabase _sqlDatabase;
    private readonly ILayananRepo _layananRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly ILogistikMedisBffDal _bffDal;
    private readonly IOrderMutasiRepo? _orderMutasiRepo;
    private readonly ITglJamProvider? _tglJamProvider;

    public LogistikMedisMutasiKeluarDispatchBatchHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        IMutasiBrgMovementService movementService,
        ISqlDatabase sqlDatabase,
        ILayananRepo layananRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        ILogistikMedisBffDal bffDal,
        IOrderMutasiRepo? orderMutasiRepo = null,
        ITglJamProvider? tglJamProvider = null)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
        _movementService = movementService;
        _sqlDatabase = sqlDatabase;
        _layananRepo = layananRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _bffDal = bffDal;
        _orderMutasiRepo = orderMutasiRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LogistikMedisMutasiKeluarDispatchBatchResponse> Handle(
        LogistikMedisMutasiKeluarDispatchBatchCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananAsalId, nameof(request.LayananAsalId));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var layananAsalId = request.LayananAsalId.Trim();
        var layananTujuanId = request.LayananTujuanId.Trim();

        if (string.Equals(layananAsalId, layananTujuanId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Unit asal dan tujuan tidak boleh sama.", nameof(request.LayananTujuanId));

        var items = (request.ListItem ?? [])
            .Where(x => x is not null)
            .ToList();
        if (items.Count == 0)
            throw new ArgumentException("Mutasi barang harus memiliki minimal 1 item untuk di-dispatch.", nameof(request.ListItem));

        foreach (var item in items)
        {
            Guard.Against.NullOrWhiteSpace(item.BrgId, nameof(item.BrgId));
            Guard.Against.NullOrWhiteSpace(item.SatuanId, nameof(item.SatuanId));
            if (item.QtyKirim <= 0m)
                throw new ArgumentException($"QtyKirim untuk barang '{item.BrgId}' harus lebih dari 0.", nameof(item.QtyKirim));
        }

        var layananAsal = GetLayanan(layananAsalId, nameof(request.LayananAsalId));
        var layananTujuan = GetLayanan(layananTujuanId, nameof(request.LayananTujuanId));

        var orderMutasiId = request.OrderMutasiId?.Trim() ?? string.Empty;
        if (orderMutasiId == "-")
            orderMutasiId = string.Empty;

        ILookup<string, LogistikMedisStokBatchRowDto>? stokBatchesByBrg = null;

        var preparedItems = new List<MutasiBrgItemModel>(items.Count);
        foreach (var item in items)
        {
            var brg = GetActiveBrg(item.BrgId.Trim());
            var satuan = GetSatuan(item.SatuanId.Trim());

            var batchNo = item.BatchNo?.Trim() ?? string.Empty;
            var expiredDate = item.ExpiredDate?.Trim() ?? string.Empty;

            var groupRekDkId = brg.GroupRekDk?.GroupRekDkId ?? string.Empty;
            var isPharmacy = string.Equals(groupRekDkId, "OBT", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(groupRekDkId, "BHP", StringComparison.OrdinalIgnoreCase);

            if (isPharmacy && string.IsNullOrWhiteSpace(expiredDate))
            {
                stokBatchesByBrg ??= (_bffDal.ListStokBatch(layananAsalId) ?? [])
                    .Where(x => x.QtySisa > 0m)
                    .ToLookup(x => x.BrgId, StringComparer.OrdinalIgnoreCase);

                var fefoBatch = stokBatchesByBrg[brg.BrgId].FirstOrDefault();
                if (fefoBatch is not null)
                {
                    if (string.IsNullOrWhiteSpace(batchNo))
                        batchNo = fefoBatch.NoBatch ?? string.Empty;
                    expiredDate = fefoBatch.TglEd ?? string.Empty;
                }
            }

            if (isPharmacy && (string.IsNullOrWhiteSpace(expiredDate) || !DateTime.TryParse(expiredDate, out _)))
                throw new ArgumentException(
                    $"ExpiredDate wajib diisi atau tersedia dari batch stok untuk barang farmasi '{brg.BrgName}' ({groupRekDkId}).",
                    nameof(item.ExpiredDate));

            preparedItems.Add(MutasiBrgItemModel.Create(
                brg: brg.ToReff(),
                batchNo: batchNo,
                expiredDate: expiredDate,
                qtyKirim: item.QtyKirim,
                satuan: satuan));
        }

        var userId = request.UserId.Trim();
        var now = _tglJamProvider?.Now ?? DateTime.Now;
        var mutasi = MutasiBrgModel.Create(
            id: string.Empty,
            date: now,
            asal: layananAsal.ToReff(),
            tujuan: layananTujuan.ToReff(),
            orderMutasiId: orderMutasiId,
            dispatchNote: request.DispatchNote ?? string.Empty,
            userId: userId);

        foreach (var preparedItem in preparedItems)
        {
            mutasi.AddItem(preparedItem);
        }

        _sqlDatabase.ExecuteTransaction((conn, trans) =>
        {
            mutasi.Dispatch(userId, now);
            _movementService.DeductStock(mutasi, conn, trans);

            if (!string.IsNullOrWhiteSpace(mutasi.OrderMutasiId) && _orderMutasiRepo is not null)
            {
                var orderKey = OrderMutasiModel.Key(mutasi.OrderMutasiId);
                var orderMaybe = _orderMutasiRepo.LoadEntity(orderKey);
                if (orderMaybe.HasValue)
                {
                    var orderMutasi = orderMaybe.Value;
                    if (orderMutasi.State == OrderMutasiStateEnum.Approved)
                    {
                        var isFulfilled = orderMutasi.ListItem.Count > 0 && orderMutasi.ListItem.All(orderItem =>
                        {
                            var dispatchedQty = mutasi.ListItem
                                .Where(x => x.Brg.BrgId == orderItem.Brg.BrgId)
                                .Sum(x => x.QtyKirim);
                            return dispatchedQty >= orderItem.Qty;
                        });

                        if (isFulfilled)
                        {
                            orderMutasi.Complete();
                            _orderMutasiRepo.SaveChanges(orderMutasi);
                        }
                    }
                }
            }

            _mutasiBrgRepo.SaveChanges(mutasi, conn, trans);
        });

        return Task.FromResult(new LogistikMedisMutasiKeluarDispatchBatchResponse(mutasi.MutasiBrgId));
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
