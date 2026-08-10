using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-006 — Post Internal Consumption Consequence (outbound dual-write PK).
/// </summary>
public record PostInternalConsumptionConsequenceCommand(
    string BrgId,
    string LayananId,
    decimal Qty,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    DateTime? TglEd = null) : IRequest<PostInternalConsumptionConsequenceResult>;

public enum PostInternalConsumptionOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    InsufficientStock = 2,
    AbortedInconsistent = 3
}

public record PostInternalConsumptionLineResult(
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuId,
    string LegacyStokId,
    decimal QtyOut);

public record PostInternalConsumptionConsequenceResult(
    PostInternalConsumptionOutcomeEnum Outcome,
    IReadOnlyList<PostInternalConsumptionLineResult> Lines,
    decimal ShortfallQty,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostInternalConsumptionConsequenceHandler
    : IRequestHandler<PostInternalConsumptionConsequenceCommand, PostInternalConsumptionConsequenceResult>
{
    private const MovementKindEnum MovementKind = MovementKindEnum.InternalConsumption;
    private const string LegacyKindString = "PK";

    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostInternalConsumptionConsequenceHandler(
        LegacyFreshnessGate freshnessGate,
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo,
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _freshnessGate = freshnessGate;
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public async Task<PostInternalConsumptionConsequenceResult> Handle(
        PostInternalConsumptionConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");

        var priorMutasi = _mutasiRepo.ListByTrsReffId(request.TrsReffId).ToList();
        if (priorMutasi.Count > 0)
        {
            if (priorMutasi.Any(x => x.MovementKind != MovementKind))
            {
                throw new InvalidOperationException(
                    $"TrsReffId={request.TrsReffId} already has non-internal-consumption movements " +
                    $"(expected only {MovementKind}).");
            }

            if (!TryValidatePriorConsumptionLines(request, priorMutasi, out var validated))
            {
                throw new InvalidOperationException(
                    $"Partial internal-consumption idempotency for TrsReffId={request.TrsReffId}: " +
                    "some InternalConsumption legs exist but qty/location do not match the request " +
                    "(unexpected after atomic UoW).");
            }

            return BuildIdempotentResult(validated);
        }

        var candidates = _batchRepo.ListAllocationCandidates(request.BrgId, request.LayananId)
            .Select(StockAllocationCandidateType.FromLokasi)
            .ToList();

        var scopeKeys = candidates
            .Select(c => new ScopeKeyDto(request.BrgId, c.BrgMasukReffId))
            .Distinct()
            .ToList();

        var freshness = await _freshnessGate.EnsureFreshAsync(
            scopeKeys,
            request.UserId,
            cancellationToken);

        if (freshness.Outcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent)
        {
            return new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.AbortedInconsistent,
                [],
                ShortfallQty: 0,
                freshness.InconsistencyReason,
                freshness.FailedBrgId,
                freshness.FailedBrgMasukReffId);
        }

        // Reload candidates after gate (hydrate/catch-up may have changed balances).
        candidates = _batchRepo.ListAllocationCandidates(request.BrgId, request.LayananId)
            .Select(StockAllocationCandidateType.FromLokasi)
            .ToList();

        var allocation = StockOutboundAllocator.Allocate(
            request.BrgId,
            request.LayananId,
            request.Qty,
            candidates,
            explicitTglEd: request.TglEd);

        if (!allocation.IsSuccess)
        {
            return new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.InsufficientStock,
                [],
                allocation.ShortfallQty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        var batchesByDo = new Dictionary<string, StockBatchModel>(StringComparer.Ordinal);
        var scopesByDo = new Dictionary<string, StockLegacyScopeModel>(StringComparer.Ordinal);
        var mutasiInserts = new List<StockMovementModel>();
        var bindingInserts = new List<StockLegacyBindingModel>();
        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostInternalConsumptionLineResult>();
        var syncedAt = DateTime.Now;

        foreach (var line in allocation.Lines)
        {
            if (!batchesByDo.TryGetValue(line.BrgMasukReffId, out var batch))
            {
                var batchMaybe = _batchRepo.LoadByNaturalKey(request.BrgId, line.BrgMasukReffId);
                if (!batchMaybe.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Stock Batch not found for BrgId={request.BrgId}, BrgMasukReffId={line.BrgMasukReffId}.");
                }

                batch = batchMaybe.Value;
                batchesByDo[line.BrgMasukReffId] = batch;
            }

            var lokasi = batch.ListLokasi.FirstOrDefault(x =>
                x.StokLokasiId == line.StokLokasiId)
                ?? throw new InvalidOperationException(
                    $"Location Stock Balance {line.StokLokasiId} not found on batch {batch.StokBatchId}.");

            var noBatch = lokasi.NoBatch;
            var hpp = batch.Hpp;

            var after = batch.DecreaseLokasi(
                request.LayananId,
                line.TglEd,
                line.QtyAllocated);

            var legacyBukuId = NunaId.NewLegacyCompact("BK");
            var legacyStokId = ResolveSourceLegacyStokId(
                request.BrgId,
                line.BrgMasukReffId,
                request.LayananId,
                line.TglEd,
                line.QtyAllocated);

            var mutasi = StockMovementModel.CreateOutbound(
                after.StokLokasiId,
                batch.StokBatchId,
                request.BrgId,
                line.BrgMasukReffId,
                request.LayananId,
                line.TglEd,
                request.TrsReffId,
                MovementKind,
                qtyOut: line.QtyAllocated,
                hpp: hpp,
                tglMutasi: request.TglMutasi,
                poReffId: batch.PoReffId);

            mutasiInserts.Add(mutasi);

            bindingInserts.Add(StockLegacyBindingModel.CreateMutasiBuku(
                mutasi.StokMutasiId,
                legacyBukuId,
                request.TrsReffId,
                after.StokLokasiId,
                legacyStokId));

            legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                request.BrgId,
                line.BrgMasukReffId,
                request.LayananId,
                QtyOut: line.QtyAllocated,
                Hpp: hpp,
                TglEd: line.TglEd,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.TrsReffId,
                MovementKindString: LegacyKindString,
                PoReffId: batch.PoReffId,
                NoBatch: noBatch,
                LegacyBukuId: legacyBukuId)));
            legacyOps.Add(new LegacyStokDepleteWriteOperation(
                new LegacyStokDepleteRequest(legacyStokId, line.QtyAllocated)));

            AdvanceScopeWatermark(
                scopesByDo,
                request.BrgId,
                line.BrgMasukReffId,
                request.TglMutasi,
                legacyBukuId,
                syncedAt);

            lineResults.Add(new PostInternalConsumptionLineResult(
                mutasi.StokMutasiId,
                after.StokLokasiId,
                batch.StokBatchId,
                line.BrgMasukReffId,
                legacyBukuId,
                legacyStokId,
                line.QtyAllocated));
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: batchesByDo.Values.ToList(),
            MutasiInserts: mutasiInserts,
            BindingInserts: bindingInserts,
            ScopeUpdates: scopesByDo.Values.ToList(),
            LegacyOperations: legacyOps,
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostInternalConsumptionConsequenceResult(
            PostInternalConsumptionOutcomeEnum.Success,
            lineResults,
            ShortfallQty: 0,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static bool TryValidatePriorConsumptionLines(
        PostInternalConsumptionConsequenceCommand request,
        IReadOnlyList<StockMovementModel> priorConsumption,
        out IReadOnlyList<StockMovementModel> validated)
    {
        validated = [];
        if (priorConsumption.Count == 0)
            return false;

        if (priorConsumption.Any(x =>
                !string.Equals(x.BrgId, request.BrgId, StringComparison.Ordinal)
                || !string.Equals(x.LayananId, request.LayananId, StringComparison.Ordinal)
                || x.QtyOut <= 0
                || x.QtyIn != 0))
        {
            return false;
        }

        if (priorConsumption.Sum(x => x.QtyOut) != request.Qty)
            return false;

        validated = priorConsumption;
        return true;
    }

    private PostInternalConsumptionConsequenceResult BuildIdempotentResult(
        IReadOnlyList<StockMovementModel> priorConsumption)
    {
        var results = new List<PostInternalConsumptionLineResult>();

        foreach (var mutasi in priorConsumption)
        {
            var binding = _bindingRepo.FindByStokMutasiId(mutasi.StokMutasiId);
            results.Add(new PostInternalConsumptionLineResult(
                mutasi.StokMutasiId,
                mutasi.StokLokasiId,
                mutasi.StokBatchId,
                mutasi.BrgMasukReffId,
                binding.HasValue ? binding.Value.LegacyBukuId : string.Empty,
                binding.HasValue ? binding.Value.LegacyStokId : string.Empty,
                mutasi.QtyOut));
        }

        return new PostInternalConsumptionConsequenceResult(
            PostInternalConsumptionOutcomeEnum.Idempotent,
            results,
            ShortfallQty: 0,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private string ResolveSourceLegacyStokId(
        string brgId,
        string brgMasukReffId,
        string layananId,
        DateTime tglEd,
        decimal qtyNeeded)
    {
        var matches = _legacyRead.ListBalances(brgId, brgMasukReffId)
            .Where(b =>
                b.LayananId == layananId
                && b.TglEd == tglEd
                && b.QtySisa >= qtyNeeded)
            .ToList();

        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one legacy stok for deplete " +
                $"(Brg={brgId}, DO={brgMasukReffId}, Layanan={layananId}, " +
                $"TglEd={tglEd:yyyy-MM-dd}, Qty>={qtyNeeded}); found {matches.Count}.");
        }

        return matches[0].LegacyStokId;
    }

    private void AdvanceScopeWatermark(
        Dictionary<string, StockLegacyScopeModel> scopesByDo,
        string brgId,
        string brgMasukReffId,
        DateTime tglMutasi,
        string legacyBukuId,
        DateTime syncedAt)
    {
        if (!scopesByDo.TryGetValue(brgMasukReffId, out var scope))
        {
            var scopeKey = StockLegacyScopeModel.Key(brgId, brgMasukReffId);
            var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
            scope = scopeMaybe.HasValue
                ? scopeMaybe.Value
                : StockLegacyScopeModel.CreateNotAligned(brgId, brgMasukReffId);
            scopesByDo[brgMasukReffId] = scope;
        }

        var candidateAfter = LegacyWatermarkHelper.IsAfterWatermark(
            tglMutasi, legacyBukuId, scope.TglMutasiLast, scope.LastLegacyBukuId);

        if (scope.AlignmentStatus is AlignmentStatusEnum.Aligned or AlignmentStatusEnum.Stale)
        {
            if (candidateAfter)
                scope.AdvanceWatermark(tglMutasi, legacyBukuId, syncedAt);
            else
                scope.AdvanceWatermark(scope.TglMutasiLast, scope.LastLegacyBukuId, syncedAt);
        }
        else
        {
            scope.MarkAligned(tglMutasi, legacyBukuId, syncedAt);
        }
    }
}
