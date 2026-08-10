using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-001 — Post Goods Receipt Consequence (inbound dual-write).
/// </summary>
public record PostGoodsReceiptConsequenceCommand(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal Qty,
    decimal Hpp,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    DateTime? TglEd = null,
    string? PoReffId = null,
    string? NoBatch = null) : IRequest<PostGoodsReceiptConsequenceResult>;

public enum PostGoodsReceiptOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    AbortedInconsistent = 2
}

public record PostGoodsReceiptConsequenceResult(
    PostGoodsReceiptOutcomeEnum Outcome,
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string LegacyBukuId,
    string LegacyStokId,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostGoodsReceiptConsequenceHandler
    : IRequestHandler<PostGoodsReceiptConsequenceCommand, PostGoodsReceiptConsequenceResult>
{
    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostGoodsReceiptConsequenceHandler(
        LegacyFreshnessGate freshnessGate,
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo,
        IStockConsequenceUnitOfWork uow)
    {
        _freshnessGate = freshnessGate;
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
        _uow = uow;
    }

    public async Task<PostGoodsReceiptConsequenceResult> Handle(
        PostGoodsReceiptConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");
        if (request.Hpp < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Hpp), "Hpp must not be negative.");

        var freshness = await _freshnessGate.EnsureFreshAsync(
            [new ScopeKeyDto(request.BrgId, request.BrgMasukReffId)],
            request.UserId,
            cancellationToken);

        if (freshness.Outcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent)
        {
            return new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.AbortedInconsistent,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                freshness.InconsistencyReason,
                freshness.FailedBrgId,
                freshness.FailedBrgMasukReffId);
        }

        var tglEd = request.TglEd ?? StockLedgerSentinel.EmptyDate;

        // Always reload from persistence — never reuse a StockBatchModel after AcceptPersisted / failed Commit.
        var batchMaybe = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
        StockBatchModel batch;
        LocationStockBalanceModel? existingLokasi = null;

        if (batchMaybe.HasValue)
        {
            batch = batchMaybe.Value;
            existingLokasi = batch.ListLokasi.FirstOrDefault(x =>
                x.LayananId == request.LayananId && x.TglEd == tglEd);

            if (existingLokasi is not null
                && _mutasiRepo.Exists(
                    request.TrsReffId,
                    MovementKindEnum.GoodsReceipt,
                    existingLokasi.StokLokasiId))
            {
                var prior = _mutasiRepo.ListByTrsReffId(request.TrsReffId)
                    .FirstOrDefault(x =>
                        x.MovementKind == MovementKindEnum.GoodsReceipt
                        && x.StokLokasiId == existingLokasi.StokLokasiId);

                var legacyBuku = string.Empty;
                var legacyStok = string.Empty;
                if (prior is not null)
                {
                    var bindingMaybe = _bindingRepo.FindByStokMutasiId(prior.StokMutasiId);
                    if (bindingMaybe.HasValue)
                    {
                        legacyBuku = bindingMaybe.Value.LegacyBukuId;
                        legacyStok = bindingMaybe.Value.LegacyStokId;
                    }
                }

                return new PostGoodsReceiptConsequenceResult(
                    PostGoodsReceiptOutcomeEnum.Idempotent,
                    prior?.StokMutasiId ?? string.Empty,
                    existingLokasi.StokLokasiId,
                    batch.StokBatchId,
                    legacyBuku,
                    legacyStok,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }
        }
        else
        {
            batch = StockBatchModel.Create(
                request.BrgId,
                request.BrgMasukReffId,
                request.Hpp,
                tglMasuk: request.TglMutasi,
                poReffId: request.PoReffId);
        }

        var lokasi = batch.IncreaseLokasi(
            request.LayananId,
            tglEd,
            request.Qty,
            request.NoBatch);

        var legacyBukuId = NunaId.NewLegacyCompact("BK");
        var legacyStokId = NunaId.NewLegacyCompact("ST");

        var mutasi = StockMovementModel.CreateInbound(
            lokasi.StokLokasiId,
            batch.StokBatchId,
            request.BrgId,
            request.BrgMasukReffId,
            request.LayananId,
            tglEd,
            request.TrsReffId,
            MovementKindEnum.GoodsReceipt,
            qtyIn: request.Qty,
            hpp: request.Hpp,
            tglMutasi: request.TglMutasi,
            poReffId: request.PoReffId);

        var binding = StockLegacyBindingModel.CreateMutasiBuku(
            mutasi.StokMutasiId,
            legacyBukuId,
            request.TrsReffId,
            lokasi.StokLokasiId,
            legacyStokId);

        var scopeKey = StockLegacyScopeModel.Key(request.BrgId, request.BrgMasukReffId);
        var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
        var scope = scopeMaybe.HasValue
            ? scopeMaybe.Value
            : StockLegacyScopeModel.CreateNotAligned(request.BrgId, request.BrgMasukReffId);

        var syncedAt = DateTime.Now;
        var candidateAfter = LegacyWatermarkHelper.IsAfterWatermark(
            request.TglMutasi, legacyBukuId, scope.TglMutasiLast, scope.LastLegacyBukuId);

        if (scope.AlignmentStatus is AlignmentStatusEnum.Aligned or AlignmentStatusEnum.Stale)
        {
            if (candidateAfter)
                scope.AdvanceWatermark(request.TglMutasi, legacyBukuId, syncedAt);
            else
                // Keep forward catch-up cursor; refresh LastSyncedAt (and clear Stale) only.
                scope.AdvanceWatermark(scope.TglMutasiLast, scope.LastLegacyBukuId, syncedAt);
        }
        else
            scope.MarkAligned(request.TglMutasi, legacyBukuId, syncedAt);

        var draft = new StockConsequenceDraft(
            BatchUpserts: [batch],
            MutasiInserts: [mutasi],
            BindingInserts: [binding],
            ScopeUpdate: scope,
            LegacyOperations:
            [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    request.BrgId,
                    request.BrgMasukReffId,
                    request.LayananId,
                    Qty: request.Qty,
                    Hpp: request.Hpp,
                    TglEd: tglEd,
                    TglMasuk: batch.TglMasuk,
                    TglMutasi: request.TglMutasi,
                    TrsReffId: request.TrsReffId,
                    MovementKindString: "DO",
                    PoReffId: request.PoReffId,
                    NoBatch: request.NoBatch,
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostGoodsReceiptConsequenceResult(
            PostGoodsReceiptOutcomeEnum.Success,
            mutasi.StokMutasiId,
            lokasi.StokLokasiId,
            batch.StokBatchId,
            legacyBukuId,
            legacyStokId,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
