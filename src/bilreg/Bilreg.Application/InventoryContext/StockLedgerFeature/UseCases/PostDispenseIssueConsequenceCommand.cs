using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// PD-02 — Post DispenseIssue Consequence (pharmacy handover removal from DTU; not SaleIssueDu).
/// </summary>
public record PostDispenseIssueConsequenceCommand(
    string BrgId,
    string LayananId,
    decimal Qty,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    DateTime? TglEd = null) : IRequest<PostDispenseIssueConsequenceResult>;

public enum PostDispenseIssueOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    InsufficientStock = 2,
    AbortedInconsistent = 3
}

public record PostDispenseIssueLineResult(
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuId,
    string LegacyStokId,
    decimal QtyOut);

public record PostDispenseIssueConsequenceResult(
    PostDispenseIssueOutcomeEnum Outcome,
    IReadOnlyList<PostDispenseIssueLineResult> Lines,
    decimal ShortfallQty,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostDispenseIssueConsequenceHandler
    : IRequestHandler<PostDispenseIssueConsequenceCommand, PostDispenseIssueConsequenceResult>
{
    private const MovementKindEnum MovementKind = MovementKindEnum.DispenseIssue;
    private const string LegacyKindString = "DI";

    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostDispenseIssueConsequenceHandler(
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

    public async Task<PostDispenseIssueConsequenceResult> Handle(
        PostDispenseIssueConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");

        var priorMutasi = _mutasiRepo.ListByTrsReffId(request.TrsReffId)
            .Where(x => string.Equals(x.BrgId, request.BrgId, StringComparison.Ordinal))
            .ToList();
        if (priorMutasi.Count > 0)
        {
            if (priorMutasi.Any(x => x.MovementKind != MovementKind))
            {
                throw new InvalidOperationException(
                    $"TrsReffId={request.TrsReffId} already has non-dispense-issue movements " +
                    $"(expected only {MovementKind}).");
            }

            if (!TryValidatePriorDispenseLines(request, priorMutasi, out var validated))
            {
                throw new InvalidOperationException(
                    $"Partial dispense-issue idempotency for TrsReffId={request.TrsReffId}: " +
                    "some DispenseIssue legs exist but qty/location do not match the request " +
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
            return new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.AbortedInconsistent,
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
            return new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.InsufficientStock,
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
        var lineResults = new List<PostDispenseIssueLineResult>();
        var legacyDepletions = new Dictionary<string, decimal>(StringComparer.Ordinal);
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

            var legacyBalances = _legacyRead.ListBalances(request.BrgId, line.BrgMasukReffId)
                .Where(b => b.LayananId == request.LayananId && b.TglEd == line.TglEd)
                .Select(b =>
                {
                    var alreadyDepleted = legacyDepletions.TryGetValue(b.LegacyStokId, out var dep) ? dep : 0m;
                    return (b.LegacyStokId, QtySisa: b.QtySisa - alreadyDepleted);
                })
                .Where(b => b.QtySisa > 0)
                .OrderBy(b => b.LegacyStokId, StringComparer.Ordinal)
                .ToList();

            if (legacyBalances.Sum(b => b.QtySisa) < line.QtyAllocated)
            {
                throw new InvalidOperationException(
                    $"Expected sufficient legacy stok for deplete " +
                    $"(Brg={request.BrgId}, DO={line.BrgMasukReffId}, Layanan={request.LayananId}, " +
                    $"TglEd={line.TglEd:yyyy-MM-dd}, QtyAllocated={line.QtyAllocated}); " +
                    $"available {legacyBalances.Sum(b => b.QtySisa)}.");
            }

            if (!scopesByDo.TryGetValue(line.BrgMasukReffId, out var scopeForWatermark))
            {
                var scopeKey = StockLegacyScopeModel.Key(request.BrgId, line.BrgMasukReffId);
                var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
                if (scopeMaybe.HasValue)
                    scopeForWatermark = scopeMaybe.Value;
            }

            var watermarkToBeat = (scopeForWatermark != null && scopeForWatermark.TglMutasiLast == request.TglMutasi)
                ? scopeForWatermark.LastLegacyBukuId
                : null;

            var remaining = line.QtyAllocated;
            foreach (var match in legacyBalances)
            {
                if (remaining <= 0)
                    break;

                var sliceQty = Math.Min(remaining, match.QtySisa);
                remaining -= sliceQty;
                legacyDepletions[match.LegacyStokId] = legacyDepletions.GetValueOrDefault(match.LegacyStokId) + sliceQty;

                var legacyBukuId = MonotonicNunaId.NewLegacyCompact("BK", watermarkToBeat);
                watermarkToBeat = legacyBukuId;

                var mutasi = StockMovementModel.CreateOutbound(
                    after.StokLokasiId,
                    batch.StokBatchId,
                    request.BrgId,
                    line.BrgMasukReffId,
                    request.LayananId,
                    line.TglEd,
                    request.TrsReffId,
                    MovementKind,
                    qtyOut: sliceQty,
                    hpp: hpp,
                    tglMutasi: request.TglMutasi,
                    poReffId: batch.PoReffId);

                mutasiInserts.Add(mutasi);

                bindingInserts.Add(StockLegacyBindingModel.CreateMutasiBuku(
                    mutasi.StokMutasiId,
                    legacyBukuId,
                    request.TrsReffId,
                    after.StokLokasiId,
                    match.LegacyStokId));

                legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                    request.BrgId,
                    line.BrgMasukReffId,
                    request.LayananId,
                    QtyOut: sliceQty,
                    Hpp: hpp,
                    TglEd: line.TglEd,
                    TglMutasi: request.TglMutasi,
                    TrsReffId: request.TrsReffId,
                    MovementKindString: LegacyKindString,
                    PoReffId: batch.PoReffId,
                    NoBatch: noBatch,
                    LegacyBukuId: legacyBukuId)));
                legacyOps.Add(new LegacyStokDepleteWriteOperation(
                    new LegacyStokDepleteRequest(match.LegacyStokId, sliceQty)));

                AdvanceScopeWatermark(
                    scopesByDo,
                    request.BrgId,
                    line.BrgMasukReffId,
                    request.TglMutasi,
                    legacyBukuId,
                    syncedAt);

                lineResults.Add(new PostDispenseIssueLineResult(
                    mutasi.StokMutasiId,
                    after.StokLokasiId,
                    batch.StokBatchId,
                    line.BrgMasukReffId,
                    legacyBukuId,
                    match.LegacyStokId,
                    sliceQty));
            }
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: batchesByDo.Values.ToList(),
            MutasiInserts: mutasiInserts,
            BindingInserts: bindingInserts,
            ScopeUpdates: scopesByDo.Values.ToList(),
            LegacyOperations: legacyOps,
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostDispenseIssueConsequenceResult(
            PostDispenseIssueOutcomeEnum.Success,
            lineResults,
            ShortfallQty: 0,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static bool TryValidatePriorDispenseLines(
        PostDispenseIssueConsequenceCommand request,
        IReadOnlyList<StockMovementModel> priorDispense,
        out IReadOnlyList<StockMovementModel> validated)
    {
        validated = [];
        if (priorDispense.Count == 0)
            return false;

        if (priorDispense.Any(x =>
                !string.Equals(x.BrgId, request.BrgId, StringComparison.Ordinal)
                || !string.Equals(x.LayananId, request.LayananId, StringComparison.Ordinal)
                || x.QtyOut <= 0
                || x.QtyIn != 0))
        {
            return false;
        }

        if (priorDispense.Sum(x => x.QtyOut) != request.Qty)
            return false;

        validated = priorDispense;
        return true;
    }

    private PostDispenseIssueConsequenceResult BuildIdempotentResult(
        IReadOnlyList<StockMovementModel> priorDispense)
    {
        var results = new List<PostDispenseIssueLineResult>();

        foreach (var mutasi in priorDispense)
        {
            var binding = _bindingRepo.FindByStokMutasiId(mutasi.StokMutasiId);
            results.Add(new PostDispenseIssueLineResult(
                mutasi.StokMutasiId,
                mutasi.StokLokasiId,
                mutasi.StokBatchId,
                mutasi.BrgMasukReffId,
                binding.HasValue ? binding.Value.LegacyBukuId : string.Empty,
                binding.HasValue ? binding.Value.LegacyStokId : string.Empty,
                mutasi.QtyOut));
        }

        return new PostDispenseIssueConsequenceResult(
            PostDispenseIssueOutcomeEnum.Idempotent,
            results,
            ShortfallQty: 0,
            string.Empty,
            string.Empty,
            string.Empty);
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
