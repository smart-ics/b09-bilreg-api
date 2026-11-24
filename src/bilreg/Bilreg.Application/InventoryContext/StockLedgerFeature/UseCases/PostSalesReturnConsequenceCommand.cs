using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;
    private readonly ISender? _sender;
    private readonly ILogger<PostSalesReturnConsequenceHandler>? _logger;

    public PostSalesReturnConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow,
        ISender? sender = null,
        ILogger<PostSalesReturnConsequenceHandler>? logger = null)
    {
        _legacyRead = legacyRead;
        _uow = uow;
        _sender = sender;
        _logger = logger;
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
        if (request.TrsReffId.Trim().Length > 10)
            throw new ArgumentException("TrsReffId must not exceed 10 characters.", nameof(request.TrsReffId));
        if (request.BrgMasukReffId.Trim().Length > 10)
            throw new ArgumentException("BrgMasukReffId must not exceed 10 characters.", nameof(request.BrgMasukReffId));
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Qty must be positive.");
        if (request.Hpp < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Hpp), "Hpp must not be negative.");

        var legacyKindString = MapReturnKind(request.ReturnKind);
        var tglEd = request.TglEd ?? StockLedgerSentinel.EmptyDate;

        var priorJournals = _legacyRead
            .ListJournalsBySource(request.TrsReffId, request.BrgId, request.LayananId, legacyKindString)
            .Where(x => x.TglEd == tglEd)
            .ToList();

        if (priorJournals.Count > 0 && priorJournals.Sum(x => x.QtyIn) == request.Qty)
        {
            return new PostSalesReturnConsequenceResult(
                PostSalesReturnOutcomeEnum.Idempotent,
                string.Empty,
                string.Empty,
                string.Empty,
                priorJournals[0].LegacyBukuId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        var legacyBukuId = NunaId.NewLegacyCompact("BK");
        var legacyStokId = NunaId.NewLegacyCompact("ST");

        var draft = new StockConsequenceDraft(
            BatchUpserts: [],
            MutasiInserts: [],
            BindingInserts: [],
            ScopeUpdates: [],
            LegacyOperations:
            [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    request.BrgId,
                    request.BrgMasukReffId,
                    request.LayananId,
                    Qty: request.Qty,
                    Hpp: request.Hpp,
                    TglEd: tglEd,
                    TglMasuk: request.TglMutasi,
                    TglMutasi: request.TglMutasi,
                    TrsReffId: request.TrsReffId,
                    MovementKindString: legacyKindString,
                    NoBatch: request.NoBatch,
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: request.UserId);

        _uow.Commit(draft);

        await PendingSettleTrigger.TryDrain(_sender, _logger, request.UserId, [(request.BrgId, request.LayananId)], cancellationToken);

        return new PostSalesReturnConsequenceResult(
            PostSalesReturnOutcomeEnum.Success,
            string.Empty,
            string.Empty,
            string.Empty,
            legacyBukuId,
            legacyStokId,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static string MapReturnKind(SalesReturnKindEnum returnKind) =>
        returnKind switch
        {
            SalesReturnKindEnum.Rj => "RJ",
            SalesReturnKindEnum.Ru => "RU",
            SalesReturnKindEnum.Rt => "RT",
            _ => throw new ArgumentOutOfRangeException(nameof(returnKind), returnKind, null)
        };
}
