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

    public CatchUpScopeFromLegacyHandler(
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

    public Task<CatchUpScopeFromLegacyResult> Handle(
        CatchUpScopeFromLegacyCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var scopeKey = StockLegacyScopeModel.Key(request.BrgId, request.BrgMasukReffId);
        var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
        if (!scopeMaybe.HasValue)
        {
            return Task.FromResult(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.NotAligned,
                AlignmentStatusEnum.NotAligned,
                "Scope is NotAligned; hydrate (UC-STL-010) is required before catch-up.",
                AppliedJournalCount: 0));
        }

        var scope = scopeMaybe.Value;

        if (scope.AlignmentStatus == AlignmentStatusEnum.Inconsistent)
        {
            return Task.FromResult(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Inconsistent,
                AlignmentStatusEnum.Inconsistent,
                scope.InconsistencyReason,
                AppliedJournalCount: 0));
        }

        if (scope.AlignmentStatus == AlignmentStatusEnum.NotAligned)
        {
            return Task.FromResult(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.NotAligned,
                AlignmentStatusEnum.NotAligned,
                "Scope is NotAligned; hydrate (UC-STL-010) is required before catch-up.",
                AppliedJournalCount: 0));
        }

        // Aligned or Stale — apply journals beyond watermark.
        var journals = _legacyRead.ListJournals(request.BrgId, request.BrgMasukReffId);
        var pending = LegacyWatermarkHelper.FilterAfterWatermark(journals, scope);

        if (pending.Count == 0)
        {
            if (scope.AlignmentStatus == AlignmentStatusEnum.Stale)
            {
                scope.AdvanceWatermark(scope.TglMutasiLast, scope.LastLegacyBukuId, DateTime.Now);
                _scopeRepo.SaveChanges(scope);
            }

            return Task.FromResult(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Idempotent,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 0));
        }

        // Empty-aligned hydrate leaves no batch; allow Create on first creatable inbound.
        // Bindings-only / outbound-only with no batch still ends Inconsistent via replayer.
        var batchMaybe = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
        var replay = _replayer.Apply(
            pending,
            request.BrgId,
            request.BrgMasukReffId,
            batchMaybe.HasValue ? batchMaybe.Value : null,
            allowCreateBatch: !batchMaybe.HasValue);

        if (replay.IsInconsistent)
        {
            _replayer.PersistInconsistent(scope, replay.Batch, replay.BatchDirty, replay.InconsistencyReason);
            return Task.FromResult(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Inconsistent,
                AlignmentStatusEnum.Inconsistent,
                replay.InconsistencyReason,
                replay.AppliedCount));
        }

        if (replay.BatchDirty && replay.Batch is not null)
            _batchRepo.SaveChanges(replay.Batch);

        var last = pending[^1];
        scope.AdvanceWatermark(last.TglMutasi, last.LegacyBukuId, DateTime.Now);
        _scopeRepo.SaveChanges(scope);

        return Task.FromResult(new CatchUpScopeFromLegacyResult(
            CatchUpScopeOutcomeEnum.Success,
            AlignmentStatusEnum.Aligned,
            string.Empty,
            replay.AppliedCount));
    }
}
