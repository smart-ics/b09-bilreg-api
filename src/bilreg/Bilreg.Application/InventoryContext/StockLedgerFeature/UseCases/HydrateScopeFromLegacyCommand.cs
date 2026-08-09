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
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;

    public HydrateScopeFromLegacyHandler(
        ILegacyStockReadPort legacyRead,
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyScopeRepo scopeRepo,
        IStockLegacyBindingRepo bindingRepo)
    {
        _legacyRead = legacyRead;
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _scopeRepo = scopeRepo;
        _bindingRepo = bindingRepo;
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
        var batchDirty = false;
        var applied = 0;

        foreach (var journal in journals)
        {
            if (!LegacyMovementKindMapper.TryMap(journal.MovementKindString, out var kind))
            {
                return FailInconsistent(
                    scope,
                    batch,
                    batchDirty,
                    applied,
                    $"Unsupported legacy MovementKind '{journal.MovementKindString}' " +
                    $"on buku {journal.LegacyBukuId} (GAP-STL-003).");
            }

            if (_bindingRepo.FindByLegacyBukuId(journal.LegacyBukuId).HasValue)
            {
                // Binding without a batch baseline must not invent a replacement from later journals.
                if (batch is null)
                {
                    var reloaded = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
                    batch = reloaded.HasValue ? reloaded.Value : null;
                }

                if (batch is null)
                {
                    return FailInconsistent(
                        scope,
                        batch: null,
                        batchDirty: false,
                        applied,
                        $"Legacy journals exist for scope ({request.BrgId}, {request.BrgMasukReffId}) " +
                        "but no StokBatch baseline is present (bindings without batch).");
                }

                continue;
            }

            if (journal.QtyIn <= 0 && journal.QtyOut <= 0)
            {
                return FailInconsistent(
                    scope,
                    batch,
                    batchDirty,
                    applied,
                    $"Legacy buku {journal.LegacyBukuId} has neither QtyIn nor QtyOut > 0.");
            }

            if (journal.QtyIn > 0 && journal.QtyOut > 0)
            {
                return FailInconsistent(
                    scope,
                    batch,
                    batchDirty,
                    applied,
                    $"Legacy buku {journal.LegacyBukuId} has both QtyIn and QtyOut > 0.");
            }

            batch ??= StockBatchModel.Create(
                request.BrgId,
                request.BrgMasukReffId,
                journal.Hpp,
                journal.TglMutasi,
                string.IsNullOrWhiteSpace(journal.PoReffId) ? null : journal.PoReffId);

            var trsReffId = string.IsNullOrWhiteSpace(journal.TrsReffId)
                ? journal.LegacyBukuId
                : journal.TrsReffId;

            LocationStockBalanceModel lokasi;
            StockMovementModel mutasi;
            try
            {
                if (journal.QtyIn > 0)
                {
                    lokasi = batch.IncreaseLokasi(
                        journal.LayananId, journal.TglEd, journal.QtyIn, journal.NoBatch);
                    mutasi = StockMovementModel.CreateInbound(
                        lokasi.StokLokasiId,
                        batch.StokBatchId,
                        batch.BrgId,
                        batch.BrgMasukReffId,
                        journal.LayananId,
                        journal.TglEd,
                        trsReffId,
                        kind,
                        journal.QtyIn,
                        journal.Hpp,
                        journal.TglMutasi,
                        journal.PoReffId);
                }
                else
                {
                    lokasi = batch.DecreaseLokasi(journal.LayananId, journal.TglEd, journal.QtyOut);
                    mutasi = StockMovementModel.CreateOutbound(
                        lokasi.StokLokasiId,
                        batch.StokBatchId,
                        batch.BrgId,
                        batch.BrgMasukReffId,
                        journal.LayananId,
                        journal.TglEd,
                        trsReffId,
                        kind,
                        journal.QtyOut,
                        journal.Hpp,
                        journal.TglMutasi,
                        journal.PoReffId);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return FailInconsistent(
                    scope,
                    batch,
                    batchDirty,
                    applied,
                    $"Failed to apply legacy buku {journal.LegacyBukuId}: {ex.Message}");
            }

            _mutasiRepo.Insert(mutasi);
            _bindingRepo.Insert(StockLegacyBindingModel.CreateMutasiBuku(
                mutasi.StokMutasiId,
                journal.LegacyBukuId,
                trsReffId,
                lokasi.StokLokasiId));
            batchDirty = true;
            applied++;
        }

        if (batchDirty && batch is not null)
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
                return FailInconsistent(
                    scope,
                    batch: null,
                    batchDirty: false,
                    applied,
                    $"Legacy journals exist for scope ({request.BrgId}, {request.BrgMasukReffId}) " +
                    "but no StokBatch baseline is present (bindings without batch).");
            }

            var last = journals[^1];
            scope.MarkAligned(last.TglMutasi, last.LegacyBukuId, lastSyncedAt);
        }

        _scopeRepo.SaveChanges(scope);

        return Task.FromResult(new HydrateScopeFromLegacyResult(
            HydrateScopeOutcomeEnum.Success,
            AlignmentStatusEnum.Aligned,
            string.Empty,
            applied));
    }

    private Task<HydrateScopeFromLegacyResult> FailInconsistent(
        StockLegacyScopeModel scope,
        StockBatchModel? batch,
        bool batchDirty,
        int applied,
        string reason)
    {
        scope.MarkInconsistent(reason);
        _scopeRepo.SaveChanges(scope);
        if (batchDirty && batch is not null)
            _batchRepo.SaveChanges(batch);

        return Task.FromResult(new HydrateScopeFromLegacyResult(
            HydrateScopeOutcomeEnum.Inconsistent,
            AlignmentStatusEnum.Inconsistent,
            reason,
            applied));
    }
}
