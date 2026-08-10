using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Shared journal apply loop for UC-STL-010 hydrate and UC-STL-011 catch-up.
/// Preserves C1 invariants: binding skip + missing batch → Inconsistent (never Create
/// replacement baseline); batchDirty only after successful apply; domain failures → Inconsistent.
/// </summary>
public sealed class LegacyScopeJournalReplayer
{
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;

    public LegacyScopeJournalReplayer(
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo)
    {
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
    }

    public sealed record ReplayResult(
        bool IsInconsistent,
        string InconsistencyReason,
        StockBatchModel? Batch,
        bool BatchDirty,
        int AppliedCount);

    /// <param name="allowCreateBatch">
    /// True for hydrate and for catch-up when no batch exists yet (empty-aligned → first inbound).
    /// False when a batch is already loaded — do not invent a replacement baseline.
    /// </param>
    public ReplayResult Apply(
        IReadOnlyList<LegacyStockJournalReadModel> journals,
        string brgId,
        string brgMasukReffId,
        StockBatchModel? batch,
        bool allowCreateBatch)
    {
        var batchDirty = false;
        var applied = 0;

        foreach (var journal in journals)
        {
            if (!LegacyMovementKindMapper.TryMap(journal.MovementKindString, out var kind))
            {
                return Fail(
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
                    var reloaded = _batchRepo.LoadByNaturalKey(brgId, brgMasukReffId);
                    batch = reloaded.HasValue ? reloaded.Value : null;
                }

                if (batch is null)
                {
                    return Fail(
                        batch: null,
                        batchDirty: false,
                        applied,
                        $"Legacy journals exist for scope ({brgId}, {brgMasukReffId}) " +
                        "but no StokBatch baseline is present (bindings without batch).");
                }

                continue;
            }

            if (journal.QtyIn <= 0 && journal.QtyOut <= 0)
            {
                return Fail(
                    batch,
                    batchDirty,
                    applied,
                    $"Legacy buku {journal.LegacyBukuId} has neither QtyIn nor QtyOut > 0.");
            }

            if (journal.QtyIn > 0 && journal.QtyOut > 0)
            {
                return Fail(
                    batch,
                    batchDirty,
                    applied,
                    $"Legacy buku {journal.LegacyBukuId} has both QtyIn and QtyOut > 0.");
            }

            if (batch is null)
            {
                if (!allowCreateBatch)
                {
                    return Fail(
                        batch: null,
                        batchDirty: false,
                        applied,
                        $"Catch-up for scope ({brgId}, {brgMasukReffId}) requires an existing " +
                        "StokBatch baseline; none is present.");
                }

                batch = StockBatchModel.Create(
                    brgId,
                    brgMasukReffId,
                    journal.Hpp,
                    journal.TglMutasi,
                    string.IsNullOrWhiteSpace(journal.PoReffId) ? null : journal.PoReffId);
            }

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
                return Fail(
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

        return new ReplayResult(
            IsInconsistent: false,
            InconsistencyReason: string.Empty,
            batch,
            batchDirty,
            applied);
    }

    public void PersistInconsistent(
        StockLegacyScopeModel scope,
        StockBatchModel? batch,
        bool batchDirty,
        string reason)
    {
        scope.MarkInconsistent(reason);
        _scopeRepo.SaveChanges(scope);
        if (batchDirty && batch is not null)
            _batchRepo.SaveChanges(batch);
    }

    private static ReplayResult Fail(
        StockBatchModel? batch,
        bool batchDirty,
        int applied,
        string reason) =>
        new(IsInconsistent: true, reason, batch, batchDirty, applied);
}
