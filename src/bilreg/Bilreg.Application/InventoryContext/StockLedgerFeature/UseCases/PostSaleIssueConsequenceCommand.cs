using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-003 — Post Sale Issue Consequence (outbound dual-write DB/DU/DT).
/// </summary>
public record PostSaleIssueConsequenceCommand(
    string BrgId,
    string LayananId,
    decimal Qty,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    SaleIssueKindEnum SaleKind,
    DateTime? TglEd = null) : IRequest<PostSaleIssueConsequenceResult>;

public enum SaleIssueKindEnum
{
    Db = 0,
    Du = 1,
    Dt = 2
}

public enum PostSaleIssueOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    InsufficientStock = 2,
    AbortedInconsistent = 3
}

public record PostSaleIssueLineResult(
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuId,
    string LegacyStokId,
    decimal QtyOut,
    string PoReffId = "",
    string NoBatch = "",
    DateTime? TglEd = null,
    decimal Hpp = 0m);

public record PostSaleIssueConsequenceResult(
    PostSaleIssueOutcomeEnum Outcome,
    IReadOnlyList<PostSaleIssueLineResult> Lines,
    decimal ShortfallQty,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostSaleIssueConsequenceHandler
    : IRequestHandler<PostSaleIssueConsequenceCommand, PostSaleIssueConsequenceResult>
{
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostSaleIssueConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public Task<PostSaleIssueConsequenceResult> Handle(
        PostSaleIssueConsequenceCommand request,
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

        var legacyKindString = MapSaleKind(request.SaleKind);

        var priorJournals = _legacyRead.ListJournalsBySource(
            request.TrsReffId,
            request.BrgId,
            request.LayananId);

        if (priorJournals.Count > 0)
        {
            if (priorJournals.Any(x => x.MovementKindString != legacyKindString))
            {
                throw new InvalidOperationException(
                    $"TrsReffId={request.TrsReffId} already has non-sale-issue movements " +
                    $"(expected only {legacyKindString}).");
            }

            var priorTotalQty = priorJournals.Sum(x => x.QtyOut);
            if (priorTotalQty != request.Qty || priorJournals.Any(x => x.QtyIn != 0))
            {
                throw new InvalidOperationException(
                    $"Partial sale-issue idempotency for TrsReffId={request.TrsReffId}: " +
                    "some SaleIssue legs exist but qty/location do not match the request.");
            }

            var idempotentLines = priorJournals
                .Select(j => new PostSaleIssueLineResult(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    j.BrgMasukReffId,
                    j.LegacyBukuId,
                    string.Empty,
                    j.QtyOut,
                    j.PoReffId,
                    j.NoBatch,
                    j.TglEd,
                    j.Hpp))
                .ToList();

            return Task.FromResult(new PostSaleIssueConsequenceResult(
                PostSaleIssueOutcomeEnum.Idempotent,
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
            return Task.FromResult(new PostSaleIssueConsequenceResult(
                PostSaleIssueOutcomeEnum.InsufficientStock,
                [],
                request.Qty - allocatedQty,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostSaleIssueLineResult>();

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
                legacyKindString,
                balance.PoReffId,
                balance.NoBatch,
                balance.SatuanId)));

            legacyOps.Add(new LegacyStokDepleteWriteOperation(new LegacyStokDepleteRequest(
                balance.LegacyStokId,
                line.QtyAllocated)));

            lineResults.Add(new PostSaleIssueLineResult(
                string.Empty,
                balance.LegacyStokId,
                balance.LegacyStokId,
                balance.BrgMasukReffId,
                string.Empty,
                balance.LegacyStokId,
                line.QtyAllocated,
                balance.PoReffId,
                balance.NoBatch,
                balance.TglEd,
                balance.Hpp));
        }

        _uow.Commit(new StockConsequenceDraft([], [], [], [], legacyOps, request.UserId));

        return Task.FromResult(new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.Success,
            lineResults,
            0m,
            string.Empty,
            string.Empty,
            string.Empty));
    }

    private static string MapSaleKind(SaleIssueKindEnum saleKind) => saleKind switch
    {
        SaleIssueKindEnum.Db => "DB",
        SaleIssueKindEnum.Du => "DU",
        SaleIssueKindEnum.Dt => "DT",
        _ => throw new ArgumentOutOfRangeException(nameof(saleKind), saleKind, "Unsupported SaleIssueKind.")
    };
}
