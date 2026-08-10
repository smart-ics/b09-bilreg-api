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

        var scopeKey = StockLegacyScopeModel.Key(request.BrgId, request.BrgMasukReffId);
        var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
        var scope = scopeMaybe.HasValue
            ? scopeMaybe.Value
            : StockLegacyScopeModel.CreateNotAligned(request.BrgId, request.BrgMasukReffId);

        if (scope.AlignmentStatus == AlignmentStatusEnum.Aligned)
        {
            return Task.FromResult(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Idempotent,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 0));
        }

        if (scope.AlignmentStatus == AlignmentStatusEnum.Inconsistent)
        {
            return Task.FromResult(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Inconsistent,
                AlignmentStatusEnum.Inconsistent,
                scope.InconsistencyReason,
                AppliedJournalCount: 0));
        }

        if (scope.AlignmentStatus == AlignmentStatusEnum.Stale)
        {
            return Task.FromResult(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.StaleNeedsCatchUp,
                AlignmentStatusEnum.Stale,
                string.Empty,
                AppliedJournalCount: 0));
        }

        // NotAligned — establish baseline from legacy journals (no legacy writes).
        var journals = _legacyRead.ListJournals(request.BrgId, request.BrgMasukReffId);
        var batchMaybe = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
        StockBatchModel? batch = batchMaybe.HasValue ? batchMaybe.Value : null;

        var replay = _replayer.Apply(
            journals,
            request.BrgId,
            request.BrgMasukReffId,
            batch,
            allowCreateBatch: true);

        if (replay.IsInconsistent)
        {
            _replayer.PersistInconsistent(scope, replay.Batch, replay.BatchDirty, replay.InconsistencyReason);
            return Task.FromResult(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Inconsistent,
                AlignmentStatusEnum.Inconsistent,
                replay.InconsistencyReason,
                replay.AppliedCount));
        }

        batch = replay.Batch;
        if (replay.BatchDirty && batch is not null)
            _batchRepo.SaveChanges(batch);

        var lastSyncedAt = DateTime.Now;
        if (journals.Count == 0)
        {
            scope.MarkAligned(
                StockLedgerSentinel.EmptyDate,
                lastLegacyBukuId: string.Empty,
                lastSyncedAt);
        }
        else
        {
            // Binding skip is not completion proof — require a persisted/loaded batch baseline.
            if (batch is null)
            {
                var reloaded = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
                batch = reloaded.HasValue ? reloaded.Value : null;
            }

            if (batch is null)
            {
                var reason =
                    $"Legacy journals exist for scope ({request.BrgId}, {request.BrgMasukReffId}) " +
                    "but no StokBatch baseline is present (bindings without batch).";
                _replayer.PersistInconsistent(scope, batch: null, batchDirty: false, reason);
                return Task.FromResult(new HydrateScopeFromLegacyResult(
                    HydrateScopeOutcomeEnum.Inconsistent,
                    AlignmentStatusEnum.Inconsistent,
                    reason,
                    replay.AppliedCount));
            }

            var last = journals[^1];
            scope.MarkAligned(last.TglMutasi, last.LegacyBukuId, lastSyncedAt);
        }

        _scopeRepo.SaveChanges(scope);

        return Task.FromResult(new HydrateScopeFromLegacyResult(
            HydrateScopeOutcomeEnum.Success,
            AlignmentStatusEnum.Aligned,
            string.Empty,
            replay.AppliedCount));
    }
}
