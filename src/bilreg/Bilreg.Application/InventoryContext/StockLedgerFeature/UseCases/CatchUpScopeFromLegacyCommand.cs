using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Microsoft.Extensions.Logging;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

public record CatchUpScopeFromLegacyCommand(
    string BrgId,
    string BrgMasukReffId,
    string UserId) : IRequest<CatchUpScopeFromLegacyResult>;

public enum CatchUpScopeOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    Inconsistent = 2,
    NotAligned = 3
}

public record CatchUpScopeFromLegacyResult(
    CatchUpScopeOutcomeEnum Outcome,
    AlignmentStatusEnum AlignmentStatus,
    string InconsistencyReason,
    int AppliedJournalCount);

/// <summary>
/// UC-STL-011 — apply legacy buku rows after scope watermark; advance watermark; no legacy writes.
/// </summary>
public class CatchUpScopeFromLegacyHandler
    : IRequestHandler<CatchUpScopeFromLegacyCommand, CatchUpScopeFromLegacyResult>
{
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly LegacyScopeJournalReplayer _replayer;
    private readonly ISender? _sender;
    private readonly ILogger<CatchUpScopeFromLegacyHandler>? _logger;

    public CatchUpScopeFromLegacyHandler(
        ILegacyStockReadPort legacyRead,
        IStockBatchRepo batchRepo,
        IStockLegacyScopeRepo scopeRepo,
        LegacyScopeJournalReplayer replayer,
        ISender? sender = null,
        ILogger<CatchUpScopeFromLegacyHandler>? logger = null)
    {
        _legacyRead = legacyRead;
        _batchRepo = batchRepo;
        _scopeRepo = scopeRepo;
        _replayer = replayer;
        _sender = sender;
        _logger = logger;
    }

    public Task<CatchUpScopeFromLegacyResult> Handle(
        CatchUpScopeFromLegacyCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // Deactivated (P1-S02): no reads/writes against BILRG_Stok* tables.
        return Task.FromResult(new CatchUpScopeFromLegacyResult(
            CatchUpScopeOutcomeEnum.Idempotent,
            AlignmentStatusEnum.Aligned,
            string.Empty,
            AppliedJournalCount: 0));
    }
}
