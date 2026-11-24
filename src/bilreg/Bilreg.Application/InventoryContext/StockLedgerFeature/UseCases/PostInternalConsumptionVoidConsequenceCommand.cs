using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-007 — Post Internal Consumption Void Consequence (reverse-journal on tb_buku + RestoreStok on tb_stok).
/// </summary>
public record PostInternalConsumptionVoidConsequenceCommand(
    string OriginalTrsReffId,
    string VoidTrsReffId,
    DateTime TglMutasi,
    string UserId) : IRequest<PostInternalConsumptionVoidConsequenceResult>
{
    public string OriginalPakaiTrsReffId => OriginalTrsReffId;
}

public enum PostInternalConsumptionVoidOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    Unbound = 2,
    AlreadyReversed = 3,
    OriginalNotFound = 4,
    AbortedInconsistent = 5
}

public record PostInternalConsumptionVoidLineResult(
    string VoidStokMutasiId,
    string OriginalStokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    string LegacyBukuId,
    string LegacyStokId,
    decimal QtyRestored);

public record PostInternalConsumptionVoidConsequenceResult(
    PostInternalConsumptionVoidOutcomeEnum Outcome,
    IReadOnlyList<PostInternalConsumptionVoidLineResult> Lines,
    string UnboundStokMutasiId,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostInternalConsumptionVoidConsequenceHandler
    : IRequestHandler<PostInternalConsumptionVoidConsequenceCommand, PostInternalConsumptionVoidConsequenceResult>
{
    private const string ConsumptionLegacyKindString = "PK";
    private const string VoidLegacyKindString = "15";

    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostInternalConsumptionVoidConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public Task<PostInternalConsumptionVoidConsequenceResult> Handle(
        PostInternalConsumptionVoidConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OriginalTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.VoidTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.VoidTrsReffId.Length > 10)
            throw new ArgumentException("VoidTrsReffId must be at most 10 characters.", nameof(request.VoidTrsReffId));

        var voidRefJournals = _legacyRead.ListJournalsBySource(request.VoidTrsReffId);
        var isSameRef = string.Equals(request.VoidTrsReffId.Trim(), request.OriginalTrsReffId.Trim(), StringComparison.Ordinal);
        var hasConflict = isSameRef
            ? voidRefJournals.Any(x => x.MovementKindString != VoidLegacyKindString && x.MovementKindString != ConsumptionLegacyKindString)
            : voidRefJournals.Any(x => x.MovementKindString != VoidLegacyKindString);

        if (hasConflict)
        {
            throw new InvalidOperationException(
                $"VoidTrsReffId={request.VoidTrsReffId} already has non-internal-consumption-void movements " +
                $"(expected only {VoidLegacyKindString}).");
        }

        var priorVoidJournals = voidRefJournals
            .Where(x => x.MovementKindString == VoidLegacyKindString)
            .ToList();

        if (priorVoidJournals.Count > 0)
        {
            var idempotentLines = priorVoidJournals
                .Select(j => new PostInternalConsumptionVoidLineResult(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    j.BrgMasukReffId,
                    j.LegacyBukuId,
                    string.Empty,
                    j.QtyIn))
                .ToList();

            return Task.FromResult(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.Idempotent,
                idempotentLines,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        var originalJournals = _legacyRead.ListJournalsBySource(request.OriginalTrsReffId)
            .Where(j => j.MovementKindString == ConsumptionLegacyKindString)
            .ToList();

        if (originalJournals.Count == 0)
        {
            return Task.FromResult(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.OriginalNotFound,
                [],
                string.Empty,
                $"No internal-consumption tb_buku rows found for OriginalTrsReffId={request.OriginalTrsReffId}.",
                string.Empty,
                string.Empty));
        }

        if (originalJournals.Any(j => j.QtyOut <= 0 || j.QtyIn != 0))
        {
            return Task.FromResult(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.AbortedInconsistent,
                [],
                string.Empty,
                $"Ambiguous tb_buku internal-consumption rows for OriginalTrsReffId={request.OriginalTrsReffId}.",
                string.Empty,
                string.Empty));
        }

        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostInternalConsumptionVoidLineResult>();

        foreach (var orig in originalJournals)
        {
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
                VoidLegacyKindString,
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

            lineResults.Add(new PostInternalConsumptionVoidLineResult(
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

        return Task.FromResult(new PostInternalConsumptionVoidConsequenceResult(
            PostInternalConsumptionVoidOutcomeEnum.Success,
            lineResults,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty));
    }
}
