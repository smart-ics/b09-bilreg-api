using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Microsoft.Extensions.Logging;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-001 — Post Goods Receipt Consequence (inbound dual-write).
/// </summary>
public record PostGoodsReceiptConsequenceCommand(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal Qty,
    decimal Hpp,
    string TrsReffId,
    DateTime TglMutasi,
    string UserId,
    DateTime? TglEd = null,
    string? PoReffId = null,
    string? NoBatch = null,
    string? SatuanId = null,
    bool AllowAdditiveLine = false) : IRequest<PostGoodsReceiptConsequenceResult>;

public enum PostGoodsReceiptOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    AbortedInconsistent = 2
}

public record PostGoodsReceiptConsequenceResult(
    PostGoodsReceiptOutcomeEnum Outcome,
    string StokMutasiId,
    string StokLokasiId,
    string StokBatchId,
    string LegacyBukuId,
    string LegacyStokId,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

public class PostGoodsReceiptConsequenceHandler
    : IRequestHandler<PostGoodsReceiptConsequenceCommand, PostGoodsReceiptConsequenceResult>
{
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;
    private readonly ISender? _sender;
    private readonly ILogger<PostGoodsReceiptConsequenceHandler>? _logger;

    public PostGoodsReceiptConsequenceHandler(
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow,
        ISender? sender = null,
        ILogger<PostGoodsReceiptConsequenceHandler>? logger = null)
    {
        _legacyRead = legacyRead;
        _uow = uow;
        _sender = sender;
        _logger = logger;
    }

    public async Task<PostGoodsReceiptConsequenceResult> Handle(
        PostGoodsReceiptConsequenceCommand request,
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

        var tglEd = request.TglEd ?? StockLedgerSentinel.EmptyDate;

        if (!request.AllowAdditiveLine)
        {
            var priorJournals = _legacyRead
                .ListJournalsBySource(request.TrsReffId, request.BrgId, request.LayananId, "DO")
                .Where(x => x.TglEd == tglEd)
                .ToList();

            if (priorJournals.Count > 0 && priorJournals.Sum(x => x.QtyIn) == request.Qty)
            {
                return new PostGoodsReceiptConsequenceResult(
                    PostGoodsReceiptOutcomeEnum.Idempotent,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    priorJournals[0].LegacyBukuId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }
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
                    MovementKindString: "DO",
                    PoReffId: request.PoReffId,
                    NoBatch: request.NoBatch,
                    SatuanId: request.SatuanId,
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: request.UserId);

        _uow.Commit(draft);

        await PendingSettleTrigger.TryDrain(_sender, _logger, request.UserId, [(request.BrgId, request.LayananId)], cancellationToken);

        return new PostGoodsReceiptConsequenceResult(
            PostGoodsReceiptOutcomeEnum.Success,
            string.Empty,
            string.Empty,
            string.Empty,
            legacyBukuId,
            legacyStokId,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
