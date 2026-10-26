using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;

public record PakaiBrgCreateItemRequest(
    string BrgId,
    decimal Qty,
    DateTime? TglEd = null,
    string KeteranganItem = "");

public record PakaiBrgCreateCommand(
    string LayananId,
    string UserId,
    string Keterangan,
    IEnumerable<PakaiBrgCreateItemRequest> ListItem
) : IRequest<PakaiBrgCreateResponse>;

public record PakaiBrgCreateResponse(
    string PakaiBrgId,
    DateTime TglPakai,
    decimal TotalNilai,
    int JumlahItem);

public class PakaiBrgCreateHandler : IRequestHandler<PakaiBrgCreateCommand, PakaiBrgCreateResponse>
{
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILayananRepo _layananRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IPakaiBrgRepo _pakaiBrgRepo;
    private readonly IMediator _mediator;

    public PakaiBrgCreateHandler(
        ITglJamProvider tglJamProvider,
        ILayananRepo layananRepo,
        IBrgRepo brgRepo,
        IPakaiBrgRepo pakaiBrgRepo,
        IMediator mediator)
    {
        _tglJamProvider = tglJamProvider;
        _layananRepo = layananRepo;
        _brgRepo = brgRepo;
        _pakaiBrgRepo = pakaiBrgRepo;
        _mediator = mediator;
    }

    public async Task<PakaiBrgCreateResponse> Handle(PakaiBrgCreateCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var itemList = request.ListItem?.ToList() ?? [];
        if (itemList.Count == 0)
            throw new ArgumentException("ListItem pemakaian barang tidak boleh kosong.", nameof(request.ListItem));

        foreach (var item in itemList)
        {
            Guard.Against.NullOrWhiteSpace(item.BrgId, nameof(item.BrgId));
            if (item.Qty <= 0)
                throw new ArgumentException($"Qty pemakaian barang '{item.BrgId}' harus lebih besar dari 0.", nameof(request.ListItem));
        }

        // 2. Validate Layanan existence and active status
        var layanan = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan with Id '{request.LayananId}' not found.")
            );

        if (!layanan.IsAktif)
            throw new InvalidOperationException($"Layanan '{layanan.LayananName}' ({request.LayananId}) tidak aktif.");

        // 3. Validate Goods existence and active status
        var brgMap = new Dictionary<string, IBrg>();
        foreach (var item in itemList)
        {
            if (brgMap.ContainsKey(item.BrgId))
                continue;

            var brg = _brgRepo.LoadEntity(new BrgKey(item.BrgId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Barang with Id '{item.BrgId}' not found.")
                );

            if (!brg.IsAktif)
                throw new InvalidOperationException($"Barang '{brg.BrgName}' ({item.BrgId}) tidak aktif.");

            brgMap[item.BrgId] = brg;
        }

        // 4. Time Authority and Document ID Generation
        var now = _tglJamProvider.Now;
        var pakaiBrgId = NunaId.NewLegacyCompact("PK");

        // 5. Atomic Stock Allocation and Document Persistence
        var pakaiItems = new List<PakaiBrgItemModel>();
        var noUrut = 1;

        using var trans = TransHelper.NewScope();

        foreach (var item in itemList)
        {
            var brg = brgMap[item.BrgId];
            var consequenceCmd = new PostInternalConsumptionConsequenceCommand(
                item.BrgId,
                layanan.LayananId,
                item.Qty,
                pakaiBrgId,
                now,
                request.UserId,
                item.TglEd);

            var consequenceResult = await _mediator.Send(consequenceCmd, cancellationToken);

            if (consequenceResult.Outcome == PostInternalConsumptionOutcomeEnum.InsufficientStock)
            {
                throw new InvalidOperationException(
                    $"Stok barang '{brg.BrgName}' ({item.BrgId}) tidak mencukupi di layanan '{layanan.LayananName}'. Kekurangan: {consequenceResult.ShortfallQty}.");
            }

            if (consequenceResult.Outcome == PostInternalConsumptionOutcomeEnum.AbortedInconsistent)
            {
                throw new InvalidOperationException(
                    $"Gagal memproses pemakaian barang '{brg.BrgName}' karena inkonsistensi: {consequenceResult.InconsistencyReason}");
            }

            if (consequenceResult.Outcome != PostInternalConsumptionOutcomeEnum.Success &&
                consequenceResult.Outcome != PostInternalConsumptionOutcomeEnum.Idempotent)
            {
                throw new InvalidOperationException(
                    $"Gagal memproses alokasi stok untuk barang '{brg.BrgName}' ({consequenceResult.Outcome}).");
            }

            foreach (var line in consequenceResult.Lines)
            {
                var tglEd = line.TglEd != default ? line.TglEd : (item.TglEd ?? new DateTime(3000, 1, 1));
                var satuan = brg.SatuanKecil?.ToReff() ?? new SatuanReff("-", "-");

                var itemModel = PakaiBrgItemModel.Create(
                    noUrut++,
                    brg.ToReff(),
                    satuan,
                    stokCurrent: line.StokCurrent,
                    qty: line.QtyOut,
                    noBatch: line.NoBatch,
                    tglEd: tglEd,
                    hpp: line.Hpp,
                    keterangan: item.KeteranganItem);

                pakaiItems.Add(itemModel);
            }
        }

        var pakaiBrgModel = PakaiBrgModel.Create(
            now,
            layanan.ToReff(),
            request.UserId,
            request.Keterangan,
            pakaiItems,
            pakaiBrgId);

        _pakaiBrgRepo.SaveChanges(pakaiBrgModel);
        trans.Complete();

        return new PakaiBrgCreateResponse(
            pakaiBrgModel.PakaiBrgId,
            pakaiBrgModel.TglPakai,
            pakaiBrgModel.TotalNilai,
            pakaiBrgModel.ListItem.Count);
    }
}
