using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature;

public class PakaiBrgInternalService : IPakaiBrgInternalService,
    IRequestHandler<PakaiBrgCreateFromTindakanCommand, PakaiBrgCreateFromTindakanResult>,
    IRequestHandler<PakaiBrgVoidFromTindakanCommand, PakaiBrgVoidFromTindakanResult>
{
    private const int MAX_KETERANGAN_LENGTH = 40;

    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILayananRepo _layananRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IPakaiBrgRepo _pakaiBrgRepo;
    private readonly ISender _sender;

    public PakaiBrgInternalService(
        ITglJamProvider tglJamProvider,
        ILayananRepo layananRepo,
        IBrgRepo brgRepo,
        IPakaiBrgRepo pakaiBrgRepo,
        ISender sender)
    {
        _tglJamProvider = tglJamProvider;
        _layananRepo = layananRepo;
        _brgRepo = brgRepo;
        _pakaiBrgRepo = pakaiBrgRepo;
        _sender = sender;
    }

    public async Task<PakaiBrgCreateFromTindakanResult> CreateFromTindakan(
        PakaiBrgCreateFromTindakanCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Guard inputs
        Guard.Against.Null(command, nameof(command));
        Guard.Against.NullOrWhiteSpace(command.RegId, nameof(command.RegId));
        Guard.Against.NullOrWhiteSpace(command.LayananId, nameof(command.LayananId));
        Guard.Against.NullOrWhiteSpace(command.UserId, nameof(command.UserId));

        var itemList = command.ListItem?.ToList() ?? [];
        if (itemList.Count == 0)
            throw new ArgumentException("ListItem pemakaian barang tidak boleh kosong.", nameof(command.ListItem));

        foreach (var item in itemList)
        {
            Guard.Against.NullOrWhiteSpace(item.BrgId, nameof(item.BrgId));
            if (item.Qty <= 0)
                throw new ArgumentException($"Qty pemakaian barang '{item.BrgId}' harus lebih besar dari 0.", nameof(command.ListItem));
        }

        // 2. Validate Layanan existence and active status
        var layanan = _layananRepo.LoadEntity(LayananType.Key(command.LayananId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan with Id '{command.LayananId}' not found.")
            );

        if (!layanan.IsAktif)
            throw new InvalidOperationException($"Layanan '{layanan.LayananName}' ({command.LayananId}) tidak aktif.");

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

        // 4. Time and Legacy Compact ID ("PK" <= 10 chars)
        var now = _tglJamProvider.Now;
        var pakaiBrgId = MonotonicNunaId.NewLegacyCompact("PK");

        // 5. Atomic Stock Allocation and Document Persistence
        var pakaiItems = new List<PakaiBrgItemModel>();
        var resultItems = new List<PakaiBrgCreateFromTindakanItemResult>();
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
                command.UserId,
                item.TglEd);

            var consequenceResult = await _sender.Send(consequenceCmd, cancellationToken);

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

            var lines = consequenceResult.Lines?.ToList() ?? [];
            if (lines.Count > 0)
            {
                foreach (var line in lines)
                {
                    var tglEd = line.TglEd != default ? line.TglEd : (item.TglEd ?? new DateTime(3000, 1, 1));
                    var satuan = brg.SatuanKecil?.ToReff() ?? new SatuanReff("-", "-");
                    var noBatch = !string.IsNullOrWhiteSpace(line.NoBatch) ? line.NoBatch : (item.NoBatch ?? string.Empty);
                    var hpp = line.Hpp > 0 ? line.Hpp : item.Hpp;

                    var itemModel = PakaiBrgItemModel.Create(
                        noUrut++,
                        brg.ToReff(),
                        satuan,
                        stokCurrent: line.StokCurrent,
                        qty: line.QtyOut,
                        noBatch: noBatch,
                        tglEd: tglEd,
                        hpp: hpp,
                        keterangan: item.KeteranganItem ?? string.Empty);

                    pakaiItems.Add(itemModel);
                    resultItems.Add(new PakaiBrgCreateFromTindakanItemResult(
                        itemModel.NoUrut,
                        item.BrgId,
                        line.QtyOut,
                        noBatch,
                        tglEd,
                        hpp,
                        itemModel.SubTotal));
                }
            }
            else
            {
                var tglEd = item.TglEd ?? new DateTime(3000, 1, 1);
                var satuan = brg.SatuanKecil?.ToReff() ?? new SatuanReff("-", "-");
                var itemModel = PakaiBrgItemModel.Create(
                    noUrut++,
                    brg.ToReff(),
                    satuan,
                    stokCurrent: 0,
                    qty: item.Qty,
                    noBatch: item.NoBatch ?? string.Empty,
                    tglEd: tglEd,
                    hpp: item.Hpp,
                    keterangan: item.KeteranganItem ?? string.Empty);

                pakaiItems.Add(itemModel);
                resultItems.Add(new PakaiBrgCreateFromTindakanItemResult(
                    itemModel.NoUrut,
                    item.BrgId,
                    item.Qty,
                    item.NoBatch ?? string.Empty,
                    tglEd,
                    item.Hpp,
                    itemModel.SubTotal));
            }
        }

        var safeKeterangan = TruncateSafe(command.KeteranganHeader, MAX_KETERANGAN_LENGTH);
        var pakaiBrgModel = PakaiBrgModel.Create(
            now,
            layanan.ToReff(),
            command.UserId,
            safeKeterangan,
            pakaiItems,
            pakaiBrgId);

        _pakaiBrgRepo.SaveChanges(pakaiBrgModel, command.RegId);
        trans.Complete();

        return new PakaiBrgCreateFromTindakanResult(
            pakaiBrgModel.PakaiBrgId,
            pakaiBrgModel.TglPakai,
            pakaiBrgModel.TotalNilai,
            pakaiBrgModel.ListItem.Count,
            resultItems);
    }

    public async Task<PakaiBrgVoidFromTindakanResult> VoidFromTindakan(
        PakaiBrgVoidFromTindakanCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Guard inputs
        Guard.Against.Null(command, nameof(command));
        Guard.Against.NullOrWhiteSpace(command.PakaiBrgId, nameof(command.PakaiBrgId));
        Guard.Against.NullOrWhiteSpace(command.UserId, nameof(command.UserId));

        var alasanVoid = string.IsNullOrWhiteSpace(command.AlasanVoid)
            ? "Batal tindakan"
            : command.AlasanVoid;

        // 2. Load existing aggregate
        var pakaiBrg = _pakaiBrgRepo.LoadEntity(PakaiBrgModel.Key(command.PakaiBrgId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen pemakaian barang dengan Id '{command.PakaiBrgId}' tidak ditemukan.")
            );

        // 3. Time Authority and domain transition
        var now = _tglJamProvider.Now;
        pakaiBrg.Void(command.UserId, alasanVoid, now);

        // 4. Atomic stock reversal ("PK_V") and document state persistence
        using var trans = TransHelper.NewScope();

        var voidConsequenceCmd = new PostInternalConsumptionVoidConsequenceCommand(
            pakaiBrg.PakaiBrgId,
            pakaiBrg.PakaiBrgId,
            now,
            command.UserId);

        var voidResult = await _sender.Send(voidConsequenceCmd, cancellationToken);

        if (voidResult.Outcome != PostInternalConsumptionVoidOutcomeEnum.Success &&
            voidResult.Outcome != PostInternalConsumptionVoidOutcomeEnum.Idempotent)
        {
            var reason = !string.IsNullOrWhiteSpace(voidResult.InconsistencyReason)
                ? voidResult.InconsistencyReason
                : voidResult.Outcome.ToString();
            throw new InvalidOperationException($"Gagal membatalkan pemakaian barang ({reason}).");
        }

        _pakaiBrgRepo.SaveChanges(pakaiBrg);
        trans.Complete();

        return new PakaiBrgVoidFromTindakanResult(
            pakaiBrg.PakaiBrgId,
            pakaiBrg.IsVoid,
            pakaiBrg.TglVoid,
            pakaiBrg.AlasanVoid);
    }

    public Task<PakaiBrgCreateFromTindakanResult> Handle(
        PakaiBrgCreateFromTindakanCommand request,
        CancellationToken cancellationToken) => CreateFromTindakan(request, cancellationToken);

    public Task<PakaiBrgVoidFromTindakanResult> Handle(
        PakaiBrgVoidFromTindakanCommand request,
        CancellationToken cancellationToken) => VoidFromTindakan(request, cancellationToken);

    public static string TruncateSafe(string? value, int maxLength)
    {
        var safe = (value ?? string.Empty).Trim();
        return safe.Length > maxLength ? safe[..maxLength] : safe;
    }
}
