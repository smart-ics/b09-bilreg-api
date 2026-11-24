using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

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
    decimal QtyOut,
    string NoBatch = "",
    DateTime TglEd = default,
    decimal Hpp = 0m,
    decimal StokCurrent = 0m);

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
    private const string LegacyKindString = "PK";

    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostInternalConsumptionConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public Task<PostInternalConsumptionConsequenceResult> Handle(
        PostInternalConsumptionConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.TrsReffId.Length > 10)
            throw new ArgumentException("TrsReffId must be at most 10 characters.", nameof(request.TrsReffId));
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");

        var priorJournals = _legacyRead.ListJournalsBySource(
            request.TrsReffId,
            request.BrgId,
            request.LayananId);

        if (priorJournals.Count > 0)
        {
            if (priorJournals.Any(x => x.MovementKindString != LegacyKindString))
            {
                throw new InvalidOperationException(
                    $"TrsReffId={request.TrsReffId} already has non-internal-consumption movements " +
                    $"(expected only {LegacyKindString}).");
            }

            var priorTotalQty = priorJournals.Sum(x => x.QtyOut);
            if (priorTotalQty != request.Qty || priorJournals.Any(x => x.QtyIn != 0))
            {
                throw new InvalidOperationException(
                    $"Partial internal-consumption idempotency for TrsReffId={request.TrsReffId}: " +
                    "some InternalConsumption legs exist but qty/location do not match the request.");
            }

            var idempotentLines = priorJournals
                .Select(j => new PostInternalConsumptionLineResult(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    j.BrgMasukReffId,
                    j.LegacyBukuId,
                    string.Empty,
                    j.QtyOut,
                    j.NoBatch,
                    j.TglEd,
                    j.Hpp))
                .ToList();

            return Task.FromResult(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Idempotent,
                idempotentLines,
                0m,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var balances = _legacyRead.ListBalancesAtLocation(request.BrgId, request.LayananId);
        var balanceByStokId = balances.ToDictionary(b => b.LegacyStokId, StringComparer.Ordinal);

        var candidates = balances
            .Select(b => new StockAllocationCandidateType(
                b.BrgId,
                b.LayananId,
                b.LegacyStokId,
                b.LegacyStokId,
                b.BrgMasukReffId,
                b.TglEd,
                b.TglMasuk,
                b.QtySisa))
            .ToList();

        var alloc = StockOutboundAllocator.Allocate(
            request.BrgId,
            request.LayananId,
            request.Qty,
            candidates,
            request.TglEd);

        if (!alloc.IsSuccess)
        {
            var allocatedQty = alloc.Lines.Sum(l => l.QtyAllocated);
            return Task.FromResult(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.InsufficientStock,
                [],
                request.Qty - allocatedQty,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostInternalConsumptionLineResult>();

        foreach (var line in alloc.Lines)
        {
            var balance = balanceByStokId[line.StokLokasiId];

            legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                request.BrgId,
                balance.BrgMasukReffId,
                request.LayananId,
                line.QtyAllocated,
                balance.Hpp,
                balance.TglEd,
                request.TglMutasi,
                request.TrsReffId,
                LegacyKindString,
                balance.PoReffId,
                balance.NoBatch,
                balance.SatuanId)));

            legacyOps.Add(new LegacyStokDepleteWriteOperation(new LegacyStokDepleteRequest(
                balance.LegacyStokId,
                line.QtyAllocated)));

            lineResults.Add(new PostInternalConsumptionLineResult(
                string.Empty,
                balance.LegacyStokId,
                balance.LegacyStokId,
                balance.BrgMasukReffId,
                string.Empty,
                balance.LegacyStokId,
                line.QtyAllocated,
                balance.NoBatch,
                balance.TglEd,
                balance.Hpp,
                balance.QtySisa - line.QtyAllocated));
        }

        _uow.Commit(new StockConsequenceDraft([], [], [], [], legacyOps, request.UserId));

        return Task.FromResult(new PostInternalConsumptionConsequenceResult(
            PostInternalConsumptionOutcomeEnum.Success,
            lineResults,
            0m,
            string.Empty,
            string.Empty,
            string.Empty));
    }
}
