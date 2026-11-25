using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

public record HydrateScopeFromLegacyCommand(
    string BrgId,
    string BrgMasukReffId,
    string UserId) : IRequest<HydrateScopeFromLegacyResult>;

public enum HydrateScopeOutcomeEnum
{
    Success = 0,
    Idempotent = 1,
    Inconsistent = 2,
    StaleNeedsCatchUp = 3
}

public record HydrateScopeFromLegacyResult(
    HydrateScopeOutcomeEnum Outcome,
    AlignmentStatusEnum AlignmentStatus,
    string InconsistencyReason,
    int AppliedJournalCount);

public class HydrateScopeFromLegacyHandler
    : IRequestHandler<HydrateScopeFromLegacyCommand, HydrateScopeFromLegacyResult>
{
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly LegacyScopeJournalReplayer _replayer;

    public HydrateScopeFromLegacyHandler(
        ILegacyStockReadPort legacyRead,
        IStockBatchRepo batchRepo,
        IStockLegacyScopeRepo scopeRepo,
        LegacyScopeJournalReplayer replayer)
    {
        _legacyRead = legacyRead;
        _batchRepo = batchRepo;
        _scopeRepo = scopeRepo;
        _replayer = replayer;
    }

    public Task<HydrateScopeFromLegacyResult> Handle(
        HydrateScopeFromLegacyCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // Deactivated (P1-S02): no reads/writes against BILRG_Stok* tables.
        return Task.FromResult(new HydrateScopeFromLegacyResult(
            HydrateScopeOutcomeEnum.Idempotent,
            AlignmentStatusEnum.Aligned,
            string.Empty,
            AppliedJournalCount: 0));
    }
}
