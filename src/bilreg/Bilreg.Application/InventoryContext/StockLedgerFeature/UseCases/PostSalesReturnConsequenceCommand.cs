using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-005 — Post Sales Return Consequence (inbound restore RJ/RU/RT; dual-write).
/// </summary>
public record PostSalesReturnConsequenceCommand(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal Qty,
    decimal Hpp,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    SalesReturnKindEnum ReturnKind,
    DateTime? TglEd = null,
    string? NoBatch = null) : IRequest<PostSalesReturnConsequenceResult>;

public enum SalesReturnKindEnum
{
    Rj = 0,
    Ru = 1,
    Rt = 2
}

public enum PostSalesReturnOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    BatchNotFound = 2,
    AbortedInconsistent = 3
}

public record PostSalesReturnConsequenceResult(
    PostSalesReturnOutcomeEnum Outcome,
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string LegacyBukuId,
    string LegacyStokId,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostSalesReturnConsequenceHandler
    : IRequestHandler<PostSalesReturnConsequenceCommand, PostSalesReturnConsequenceResult>
{
    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostSalesReturnConsequenceHandler(
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

    public async Task<PostSalesReturnConsequenceResult> Handle(
        PostSalesReturnConsequenceCommand request,
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

        var (movementKind, legacyKindString) = MapReturnKind(request.ReturnKind);

        var freshness = await _freshnessGate.EnsureFreshAsync(
            [new ScopeKeyDto(request.BrgId, request.BrgMasukReffId)],
            request.UserId,
            cancellationToken);

        if (freshness.Outcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent)
        {
            return new PostSalesReturnConsequenceResult(
                PostSalesReturnOutcomeEnum.AbortedInconsistent,
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
        if (!batchMaybe.HasValue)
        {
            return new PostSalesReturnConsequenceResult(
                PostSalesReturnOutcomeEnum.BatchNotFound,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                request.BrgId,
                request.BrgMasukReffId);
        }

        var batch = batchMaybe.Value;
        var existingLokasi = batch.ListLokasi.FirstOrDefault(x =>
            x.LayananId == request.LayananId && x.TglEd == tglEd);

        if (existingLokasi is not null
            && _mutasiRepo.Exists(request.TrsReffId, movementKind, existingLokasi.StokLokasiId))
        {
            var prior = _mutasiRepo.ListByTrsReffId(request.TrsReffId)
                .FirstOrDefault(x =>
                    x.MovementKind == movementKind
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

            return new PostSalesReturnConsequenceResult(
                PostSalesReturnOutcomeEnum.Idempotent,
                prior?.StokMutasiId ?? string.Empty,
                existingLokasi.StokLokasiId,
                batch.StokBatchId,
                legacyBuku,
                legacyStok,
                string.Empty,
                string.Empty,
                string.Empty);
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
            movementKind,
            qtyIn: request.Qty,
            hpp: batch.Hpp,
            tglMutasi: request.TglMutasi,
            poReffId: batch.PoReffId);

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
            ScopeUpdates: [scope],
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
                    MovementKindString: legacyKindString,
                    PoReffId: batch.PoReffId,
                    NoBatch: request.NoBatch,
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostSalesReturnConsequenceResult(
            PostSalesReturnOutcomeEnum.Success,
            mutasi.StokMutasiId,
            lokasi.StokLokasiId,
            batch.StokBatchId,
            legacyBukuId,
            legacyStokId,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static (MovementKindEnum Kind, string LegacyString) MapReturnKind(
        SalesReturnKindEnum returnKind) =>
        returnKind switch
        {
            SalesReturnKindEnum.Rj => (MovementKindEnum.SalesReturnRj, "RJ"),
            SalesReturnKindEnum.Ru => (MovementKindEnum.SalesReturnRu, "RU"),
            SalesReturnKindEnum.Rt => (MovementKindEnum.SalesReturnRt, "RT"),
            _ => throw new ArgumentOutOfRangeException(nameof(returnKind), returnKind, null)
        };
}
