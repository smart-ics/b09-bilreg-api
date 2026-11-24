using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-004 — Post Sale Void Consequence (reverse-journal on tb_buku + RestoreStok on tb_stok).
/// </summary>
public record PostSaleVoidConsequenceCommand(
    string OriginalSaleTrsReffId,
    string VoidTrsReffId,
    DateTime TglMutasi,
    string UserId) : IRequest<PostSaleVoidConsequenceResult>;

public enum PostSaleVoidOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    Unbound = 2,
    AlreadyReversed = 3,
    OriginalNotFound = 4,
    AbortedInconsistent = 5
}

public record PostSaleVoidLineResult(
    string VoidStokMutasiId,
    string OriginalStokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuId,
    string LegacyStokId,
    decimal QtyRestored);

public record PostSaleVoidConsequenceResult(
    PostSaleVoidOutcomeEnum Outcome,
    IReadOnlyList<PostSaleVoidLineResult> Lines,
    string UnboundStokMutasiId,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostSaleVoidConsequenceHandler
    : IRequestHandler<PostSaleVoidConsequenceCommand, PostSaleVoidConsequenceResult>
{
    private static readonly HashSet<string> SaleIssueKinds = new(StringComparer.Ordinal) { "DB", "DU", "DT" };
    private static readonly HashSet<string> SaleVoidKinds = new(StringComparer.Ordinal) { "11", "12", "13" };

    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostSaleVoidConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public Task<PostSaleVoidConsequenceResult> Handle(
        PostSaleVoidConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OriginalSaleTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.VoidTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.VoidTrsReffId.Length > 10)
            throw new ArgumentException("VoidTrsReffId must be at most 10 characters.", nameof(request.VoidTrsReffId));

        var priorVoidJournals = _legacyRead.ListJournalsBySource(request.VoidTrsReffId);
        if (priorVoidJournals.Count > 0)
        {
            if (priorVoidJournals.Any(x => !SaleVoidKinds.Contains(x.MovementKindString)))
            {
                throw new InvalidOperationException(
                    $"VoidTrsReffId={request.VoidTrsReffId} already has non-sale-void movements.");
            }

            var idempotentLines = priorVoidJournals
                .Select(j => new PostSaleVoidLineResult(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    j.BrgMasukReffId,
                    j.LegacyBukuId,
                    string.Empty,
                    j.QtyIn))
                .ToList();

            return Task.FromResult(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.Idempotent,
                idempotentLines,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var originalJournals = _legacyRead.ListJournalsBySource(request.OriginalSaleTrsReffId)
            .Where(j => SaleIssueKinds.Contains(j.MovementKindString))
            .ToList();

        if (originalJournals.Count == 0)
        {
            return Task.FromResult(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.OriginalNotFound,
                [],
                string.Empty,
                $"No sale-issue tb_buku rows found for OriginalSaleTrsReffId={request.OriginalSaleTrsReffId}.",
                string.Empty,
                string.Empty));
        }

        if (originalJournals.Any(j => j.QtyOut <= 0 || j.QtyIn != 0))
        {
            return Task.FromResult(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.AbortedInconsistent,
                [],
                string.Empty,
                $"Ambiguous tb_buku sale-issue rows for OriginalSaleTrsReffId={request.OriginalSaleTrsReffId}.",
                string.Empty,
                string.Empty));
        }

        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostSaleVoidLineResult>();

        foreach (var orig in originalJournals)
        {
            var voidKindString = MapVoidKind(orig.MovementKindString);

            legacyOps.Add(new LegacyReverseBukuWriteOperation(new LegacyReverseBukuWriteRequest(
                orig.BrgId,
                orig.BrgMasukReffId,
                orig.LayananId,
                orig.QtyOut,
                0m,
                orig.Hpp,
                orig.TglEd,
                request.TglMutasi,
                request.VoidTrsReffId,
                voidKindString,
                orig.PoReffId,
                orig.NoBatch,
                orig.SatuanId)));

            legacyOps.Add(new LegacyStokRestoreWriteOperation(new LegacyStokRestoreRequest(
                string.Empty,
                orig.QtyOut,
                orig.BrgId,
                orig.BrgMasukReffId,
                orig.LayananId,
                orig.Hpp,
                orig.TglEd,
                orig.TglMutasi,
                request.TglMutasi,
                request.VoidTrsReffId,
                orig.PoReffId,
                orig.NoBatch,
                orig.SatuanId)));

            lineResults.Add(new PostSaleVoidLineResult(
                string.Empty,
                orig.LegacyBukuId,
                string.Empty,
                string.Empty,
                orig.BrgMasukReffId,
                orig.LegacyBukuId,
                string.Empty,
                orig.QtyOut));
        }

        _uow.Commit(new StockConsequenceDraft([], [], [], [], legacyOps, request.UserId));

        return Task.FromResult(new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.Success,
            lineResults,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty));
    }

    private static string MapVoidKind(string issueKind) => issueKind switch
    {
        "DB" => "11",
        "DU" => "12",
        "DT" => "13",
        _ => throw new InvalidOperationException($"Unsupported sale issue kind '{issueKind}' for void.")
    };
}
