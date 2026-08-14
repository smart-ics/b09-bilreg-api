using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-002 — Post Stock Transfer Consequence (paired MT_OUT + MT_IN dual-write).
/// </summary>
public record PostStockTransferConsequenceCommand(
    string BrgId,
    decimal Qty,
    string SourceLayananId,
    string DestLayananId,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    DateTime? TglEd = null,
    IReadOnlyList<string>? BrgMasukReffIds = null) : IRequest<PostStockTransferConsequenceResult>;

public enum PostStockTransferOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    InsufficientStock = 2,
    AbortedInconsistent = 3
}

public record PostStockTransferLineResult(
    string StokMutasiOutId,
    string StokMutasiInId,
    string SourceStokLokasiId,
    string DestStokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuOutId,
    string LegacyBukuInId,
    string LegacyStokInId);

public record PostStockTransferConsequenceResult(
    PostStockTransferOutcomeEnum Outcome,
    IReadOnlyList<PostStockTransferLineResult> Lines,
    decimal ShortfallQty,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostStockTransferConsequenceHandler
    : IRequestHandler<PostStockTransferConsequenceCommand, PostStockTransferConsequenceResult>
{
    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostStockTransferConsequenceHandler(
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

    public async Task<PostStockTransferConsequenceResult> Handle(
        PostStockTransferConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.SourceLayananId);
        Guard.Against.NullOrWhiteSpace(request.DestLayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");
        if (string.Equals(request.SourceLayananId, request.DestLayananId, StringComparison.Ordinal))
            throw new ArgumentException("Source and destination LayananId must differ.");

        var priorMutasi = _mutasiRepo.ListByTrsReffId(request.TrsReffId).ToList();
        var priorOuts = priorMutasi
            .Where(x => x.MovementKind == MovementKindEnum.TransferOut)
            .ToList();
        var priorIns = priorMutasi
            .Where(x => x.MovementKind == MovementKindEnum.TransferIn)
            .ToList();

        if (priorOuts.Count > 0 || priorIns.Count > 0)
        {
            if (!TryPairTransferLegs(request, priorOuts, priorIns, out var pairedLegs))
            {
                throw new InvalidOperationException(
                    $"Partial transfer idempotency for TrsReffId={request.TrsReffId}: " +
                    "some TransferOut/TransferIn legs exist but not all (unexpected after atomic UoW).");
            }

            return BuildIdempotentResult(pairedLegs);
        }

        var candidates = _batchRepo.ListAllocationCandidates(request.BrgId, request.SourceLayananId)
            .Select(StockAllocationCandidateType.FromLokasi)
            .ToList();

        if (request.BrgMasukReffIds is { Count: > 0 })
        {
            var allowed = request.BrgMasukReffIds.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(c => allowed.Contains(c.BrgMasukReffId)).ToList();
        }

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
            return new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.AbortedInconsistent,
                [],
                ShortfallQty: 0,
                freshness.InconsistencyReason,
                freshness.FailedBrgId,
                freshness.FailedBrgMasukReffId);
        }

        // Reload candidates after gate (hydrate/catch-up may have changed balances).
        candidates = _batchRepo.ListAllocationCandidates(request.BrgId, request.SourceLayananId)
            .Select(StockAllocationCandidateType.FromLokasi)
            .ToList();
        if (request.BrgMasukReffIds is { Count: > 0 })
        {
            var allowed = request.BrgMasukReffIds.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(c => allowed.Contains(c.BrgMasukReffId)).ToList();
        }

        var allocation = StockOutboundAllocator.Allocate(
            request.BrgId,
            request.SourceLayananId,
            request.Qty,
            candidates,
            explicitTglEd: request.TglEd);

        if (!allocation.IsSuccess)
        {
            return new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.InsufficientStock,
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
        var lineResults = new List<PostStockTransferLineResult>();
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

            var sourceLokasi = batch.ListLokasi.FirstOrDefault(x =>
                x.StokLokasiId == line.StokLokasiId)
                ?? throw new InvalidOperationException(
                    $"Source Location Stock Balance {line.StokLokasiId} not found on batch {batch.StokBatchId}.");

            var noBatch = sourceLokasi.NoBatch;
            var hpp = batch.Hpp;
            var hospitalBefore = batch.QtySisa;

            var sourceAfter = batch.DecreaseLokasi(
                request.SourceLayananId,
                line.TglEd,
                line.QtyAllocated);
            var destLokasi = batch.IncreaseLokasi(
                request.DestLayananId,
                line.TglEd,
                line.QtyAllocated,
                noBatch);

            if (batch.QtySisa != hospitalBefore)
            {
                throw new InvalidOperationException(
                    "Stock Transfer must not change hospital-wide Remaining Quantity (BR-STL-009).");
            }

            var legacyBukuOutId = NunaId.NewLegacyCompact("BK");
            var legacyBukuInId = NunaId.NewLegacyCompact("BK");
            var legacyStokInId = NunaId.NewLegacyCompact("ST");
            var legacyStokOutId = ResolveSourceLegacyStokId(
                request.BrgId,
                line.BrgMasukReffId,
                request.SourceLayananId,
                line.TglEd,
                line.QtyAllocated);

            var mutasiOut = StockMovementModel.CreateOutbound(
                sourceAfter.StokLokasiId,
                batch.StokBatchId,
                request.BrgId,
                line.BrgMasukReffId,
                request.SourceLayananId,
                line.TglEd,
                request.TrsReffId,
                MovementKindEnum.TransferOut,
                qtyOut: line.QtyAllocated,
                hpp: hpp,
                tglMutasi: request.TglMutasi,
                poReffId: batch.PoReffId);

            var mutasiIn = StockMovementModel.CreateInbound(
                destLokasi.StokLokasiId,
                batch.StokBatchId,
                request.BrgId,
                line.BrgMasukReffId,
                request.DestLayananId,
                line.TglEd,
                request.TrsReffId,
                MovementKindEnum.TransferIn,
                qtyIn: line.QtyAllocated,
                hpp: hpp,
                tglMutasi: request.TglMutasi,
                poReffId: batch.PoReffId);

            mutasiInserts.Add(mutasiOut);
            mutasiInserts.Add(mutasiIn);

            bindingInserts.Add(StockLegacyBindingModel.CreateMutasiBuku(
                mutasiOut.StokMutasiId,
                legacyBukuOutId,
                request.TrsReffId,
                sourceAfter.StokLokasiId,
                legacyStokOutId));
            bindingInserts.Add(StockLegacyBindingModel.CreateMutasiBuku(
                mutasiIn.StokMutasiId,
                legacyBukuInId,
                request.TrsReffId,
                destLokasi.StokLokasiId,
                legacyStokInId));

            legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                request.BrgId,
                line.BrgMasukReffId,
                request.SourceLayananId,
                QtyOut: line.QtyAllocated,
                Hpp: hpp,
                TglEd: line.TglEd,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.TrsReffId,
                MovementKindString: "MT_OUT",
                PoReffId: batch.PoReffId,
                NoBatch: noBatch,
                LegacyBukuId: legacyBukuOutId)));
            legacyOps.Add(new LegacyStokDepleteWriteOperation(
                new LegacyStokDepleteRequest(legacyStokOutId, line.QtyAllocated)));
            legacyOps.Add(new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                request.BrgId,
                line.BrgMasukReffId,
                request.DestLayananId,
                Qty: line.QtyAllocated,
                Hpp: hpp,
                TglEd: line.TglEd,
                TglMasuk: batch.TglMasuk,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.TrsReffId,
                MovementKindString: "MT_IN",
                PoReffId: batch.PoReffId,
                NoBatch: noBatch,
                LegacyBukuId: legacyBukuInId,
                LegacyStokId: legacyStokInId)));

            var watermarkBukuId = LegacyWatermarkHelper.IsAfterWatermark(
                    request.TglMutasi, legacyBukuInId, request.TglMutasi, legacyBukuOutId)
                ? legacyBukuInId
                : legacyBukuOutId;

            AdvanceScopeWatermark(
                scopesByDo,
                request.BrgId,
                line.BrgMasukReffId,
                request.TglMutasi,
                watermarkBukuId,
                syncedAt);

            lineResults.Add(new PostStockTransferLineResult(
                mutasiOut.StokMutasiId,
                mutasiIn.StokMutasiId,
                sourceAfter.StokLokasiId,
                destLokasi.StokLokasiId,
                batch.StokBatchId,
                line.BrgMasukReffId,
                legacyBukuOutId,
                legacyBukuInId,
                legacyStokInId));
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: batchesByDo.Values.ToList(),
            MutasiInserts: mutasiInserts,
            BindingInserts: bindingInserts,
            ScopeUpdates: scopesByDo.Values.ToList(),
            LegacyOperations: legacyOps,
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostStockTransferConsequenceResult(
            PostStockTransferOutcomeEnum.Success,
            lineResults,
            ShortfallQty: 0,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static bool TryPairTransferLegs(
        PostStockTransferConsequenceCommand request,
        IReadOnlyList<StockMovementModel> priorOuts,
        IReadOnlyList<StockMovementModel> priorIns,
        out IReadOnlyList<(StockMovementModel Out, StockMovementModel In)> pairedLegs)
    {
        pairedLegs = [];
        if (priorOuts.Count == 0 || priorOuts.Count != priorIns.Count)
            return false;

        var remainingIns = priorIns.ToList();
        var pairs = new List<(StockMovementModel Out, StockMovementModel In)>();

        foreach (var outMutasi in priorOuts)
        {
            if (!string.Equals(outMutasi.LayananId, request.SourceLayananId, StringComparison.Ordinal))
                return false;

            var matchIndex = remainingIns.FindIndex(inMutasi =>
                string.Equals(inMutasi.LayananId, request.DestLayananId, StringComparison.Ordinal)
                && string.Equals(inMutasi.BrgMasukReffId, outMutasi.BrgMasukReffId, StringComparison.Ordinal)
                && inMutasi.QtyIn == outMutasi.QtyOut
                && inMutasi.TglEd == outMutasi.TglEd);

            if (matchIndex < 0)
                return false;

            pairs.Add((outMutasi, remainingIns[matchIndex]));
            remainingIns.RemoveAt(matchIndex);
        }

        if (remainingIns.Count != 0)
            return false;

        pairedLegs = pairs;
        return true;
    }

    private PostStockTransferConsequenceResult BuildIdempotentResult(
        IReadOnlyList<(StockMovementModel Out, StockMovementModel In)> pairedLegs)
    {
        var results = new List<PostStockTransferLineResult>();

        foreach (var (outMutasi, inMutasi) in pairedLegs)
        {
            var outBinding = _bindingRepo.FindByStokMutasiId(outMutasi.StokMutasiId);
            var inBinding = _bindingRepo.FindByStokMutasiId(inMutasi.StokMutasiId);

            results.Add(new PostStockTransferLineResult(
                outMutasi.StokMutasiId,
                inMutasi.StokMutasiId,
                outMutasi.StokLokasiId,
                inMutasi.StokLokasiId,
                outMutasi.StokBatchId,
                outMutasi.BrgMasukReffId,
                outBinding.HasValue ? outBinding.Value.LegacyBukuId : string.Empty,
                inBinding.HasValue ? inBinding.Value.LegacyBukuId : string.Empty,
                inBinding.HasValue ? inBinding.Value.LegacyStokId : string.Empty));
        }

        return new PostStockTransferConsequenceResult(
            PostStockTransferOutcomeEnum.Idempotent,
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
