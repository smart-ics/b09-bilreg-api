using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;
    private readonly ISender? _sender;
    private readonly ILogger<PostStockTransferConsequenceHandler>? _logger;

    public PostStockTransferConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow,
        ISender? sender = null,
        ILogger<PostStockTransferConsequenceHandler>? logger = null)
    {
        _legacyRead = legacyRead;
        _uow = uow;
        _sender = sender;
        _logger = logger;
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
        if (request.TrsReffId.Trim().Length > 10)
            throw new ArgumentException("TrsReffId must not exceed 10 characters.", nameof(request.TrsReffId));
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");
        if (string.Equals(request.SourceLayananId, request.DestLayananId, StringComparison.Ordinal))
            throw new ArgumentException("Source and destination LayananId must differ.");

        var priorOuts = _legacyRead.ListJournalsBySource(
            request.TrsReffId, request.BrgId, request.SourceLayananId, "MT_OUT");
        var priorIns = _legacyRead.ListJournalsBySource(
            request.TrsReffId, request.BrgId, request.DestLayananId, "MT_IN");

        if (priorOuts.Count > 0 || priorIns.Count > 0)
        {
            if (priorOuts.Sum(x => x.QtyOut) != request.Qty
                || priorIns.Sum(x => x.QtyIn) != request.Qty
                || priorOuts.Count != priorIns.Count)
            {
                throw new InvalidOperationException(
                    $"Partial or conflicting transfer idempotency for TrsReffId={request.TrsReffId}.");
            }

            var idempotentLines = PairIdempotentLines(priorOuts, priorIns);
            return new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.Idempotent,
                idempotentLines,
                0m,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        var sourceBalances = _legacyRead.ListBalancesAtLocation(request.BrgId, request.SourceLayananId);
        if (request.BrgMasukReffIds is { Count: > 0 })
        {
            var allowed = request.BrgMasukReffIds.ToHashSet(StringComparer.Ordinal);
            sourceBalances = sourceBalances.Where(x => allowed.Contains(x.BrgMasukReffId)).ToList();
        }

        var candidates = sourceBalances
            .Where(b => b.QtySisa > 0)
            .Select(b => new StockAllocationCandidateType(
                b.LegacyStokId,
                b.BrgMasukReffId,
                b.BrgId,
                b.BrgMasukReffId,
                b.LayananId,
                b.TglEd,
                b.TglMasuk,
                b.QtySisa))
            .ToList();

        var allocation = StockOutboundAllocator.Allocate(
            request.BrgId,
            request.SourceLayananId,
            request.Qty,
            candidates,
            request.TglEd);

        if (allocation.ShortfallQty > 0)
        {
            return new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.InsufficientStock,
                [],
                allocation.ShortfallQty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        var balanceByStokId = sourceBalances.ToDictionary(x => x.LegacyStokId, StringComparer.Ordinal);
        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostStockTransferLineResult>();

        foreach (var line in allocation.Lines)
        {
            var src = balanceByStokId[line.StokLokasiId];
            var legacyBukuOutId = NunaId.NewLegacyCompact("BK");
            var legacyBukuInId = NunaId.NewLegacyCompact("BK");
            var legacyStokInId = NunaId.NewLegacyCompact("ST");

            legacyOps.Add(new LegacyStokDepleteWriteOperation(
                new LegacyStokDepleteRequest(src.LegacyStokId, line.QtyAllocated)));

            legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                request.BrgId,
                src.BrgMasukReffId,
                request.SourceLayananId,
                QtyOut: line.QtyAllocated,
                Hpp: src.Hpp,
                TglEd: src.TglEd,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.TrsReffId,
                MovementKindString: "MT_OUT",
                PoReffId: src.PoReffId,
                NoBatch: src.NoBatch,
                SatuanId: src.SatuanId,
                LegacyBukuId: legacyBukuOutId)));

            legacyOps.Add(new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                request.BrgId,
                src.BrgMasukReffId,
                request.DestLayananId,
                Qty: line.QtyAllocated,
                Hpp: src.Hpp,
                TglEd: src.TglEd,
                TglMasuk: src.TglMasuk,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.TrsReffId,
                MovementKindString: "MT_IN",
                PoReffId: src.PoReffId,
                NoBatch: src.NoBatch,
                SatuanId: src.SatuanId,
                LegacyBukuId: legacyBukuInId,
                LegacyStokId: legacyStokInId)));

            lineResults.Add(new PostStockTransferLineResult(
                string.Empty,
                string.Empty,
                src.LegacyStokId,
                string.Empty,
                string.Empty,
                src.BrgMasukReffId,
                legacyBukuOutId,
                legacyBukuInId,
                legacyStokInId));
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: [],
            MutasiInserts: [],
            BindingInserts: [],
            ScopeUpdates: [],
            LegacyOperations: legacyOps,
            UserId: request.UserId);

        _uow.Commit(draft);

        await PendingSettleTrigger.TryDrain(_sender, _logger, request.UserId, [(request.BrgId, request.DestLayananId)], cancellationToken);

        return new PostStockTransferConsequenceResult(
            PostStockTransferOutcomeEnum.Success,
            lineResults,
            0m,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static IReadOnlyList<PostStockTransferLineResult> PairIdempotentLines(
        IReadOnlyList<LegacyStockJournalReadModel> outs,
        IReadOnlyList<LegacyStockJournalReadModel> ins)
    {
        var remainingIns = ins.ToList();
        var results = new List<PostStockTransferLineResult>(outs.Count);
        foreach (var outLeg in outs)
        {
            var matchIdx = remainingIns.FindIndex(i =>
                i.BrgMasukReffId == outLeg.BrgMasukReffId
                && i.TglEd == outLeg.TglEd
                && i.QtyIn == outLeg.QtyOut);
            if (matchIdx < 0)
                matchIdx = 0;
            var inLeg = remainingIns[matchIdx];
            remainingIns.RemoveAt(matchIdx);

            results.Add(new PostStockTransferLineResult(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                outLeg.BrgMasukReffId,
                outLeg.LegacyBukuId,
                inLeg.LegacyBukuId,
                string.Empty));
        }

        return results;
    }
}
