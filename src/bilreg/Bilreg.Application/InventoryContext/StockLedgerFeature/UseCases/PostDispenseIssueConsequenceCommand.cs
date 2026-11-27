using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
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
    private const string LegacyKindString = "DI";

    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostDispenseIssueConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public Task<PostDispenseIssueConsequenceResult> Handle(
        PostDispenseIssueConsequenceCommand request,
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
                    $"TrsReffId={request.TrsReffId} already has non-dispense-issue movements " +
                    $"(expected only {LegacyKindString}).");
            }

            var priorTotalQty = priorJournals.Sum(x => x.QtyOut);
            if (priorTotalQty != request.Qty || priorJournals.Any(x => x.QtyIn != 0))
            {
                throw new InvalidOperationException(
                    $"Partial dispense-issue idempotency for TrsReffId={request.TrsReffId}: " +
                    "some DispenseIssue legs exist but qty/location do not match the request.");
            }

            var idempotentLines = priorJournals
                .Select(j => new PostDispenseIssueLineResult(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    j.BrgMasukReffId,
                    j.LegacyBukuId,
                    string.Empty,
                    j.QtyOut))
                .ToList();

            return Task.FromResult(new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.Idempotent,
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
            return Task.FromResult(new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.InsufficientStock,
                [],
                request.Qty - allocatedQty,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostDispenseIssueLineResult>();

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

            lineResults.Add(new PostDispenseIssueLineResult(
                string.Empty,
                balance.LegacyStokId,
                balance.LegacyStokId,
                balance.BrgMasukReffId,
                string.Empty,
                balance.LegacyStokId,
                line.QtyAllocated));
        }

        _uow.Commit(new StockConsequenceDraft([], [], [], [], legacyOps, request.UserId));

        return Task.FromResult(new PostDispenseIssueConsequenceResult(
            PostDispenseIssueOutcomeEnum.Success,
            lineResults,
            0m,
            string.Empty,
            string.Empty,
            string.Empty));
    }
}
