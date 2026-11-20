using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

public record StockAdjustmentItemRequest(
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal AdjustmentQty,
    decimal Hpp,
    string SatuanId,
    string ExecutedBy);

public record PostStockAdjustmentConsequenceCommand(
    string SessionId,
    string LayananId,
    IEnumerable<StockAdjustmentItemRequest> Items) : IRequest<PostStockAdjustmentConsequenceResult>;

public enum PostStockAdjustmentOutcomeEnum
{
    Success = 0,
    InsufficientStock = 1,
    Failed = 2
}

public record PostStockAdjustmentConsequenceResult(
    PostStockAdjustmentOutcomeEnum Outcome,
    string SessionId,
    IReadOnlyList<StockOpnameAdjustmentModel> Adjustments,
    string ErrorMessage = "");

public class PostStockAdjustmentConsequenceHandler
    : IRequestHandler<PostStockAdjustmentConsequenceCommand, PostStockAdjustmentConsequenceResult>
{
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;
    private readonly IStockOpnameAdjustmentRepo _adjustmentRepo;
    private readonly LegacyFreshnessGate? _freshnessGate;

    public PostStockAdjustmentConsequenceHandler(
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo,
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow,
        IStockOpnameAdjustmentRepo adjustmentRepo,
        LegacyFreshnessGate? freshnessGate = null)
    {
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
        _legacyRead = legacyRead;
        _uow = uow;
        _adjustmentRepo = adjustmentRepo;
        _freshnessGate = freshnessGate;
    }

    public async Task<PostStockAdjustmentConsequenceResult> Handle(
        PostStockAdjustmentConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var itemsToAdjust = (request.Items ?? [])
            .Where(x => x.AdjustmentQty != 0)
            .ToList();

        if (itemsToAdjust.Count == 0)
        {
            return new PostStockAdjustmentConsequenceResult(
                PostStockAdjustmentOutcomeEnum.Success,
                request.SessionId,
                []);
        }

        using var trans = TransHelper.NewScope();

        var batchesByNaturalKey = new Dictionary<(string BrgId, string DoId), StockBatchModel>();
        var scopesByNaturalKey = new Dictionary<(string BrgId, string DoId), StockLegacyScopeModel>();
        var mutasiInserts = new List<StockMovementModel>();
        var bindingInserts = new List<StockLegacyBindingModel>();
        var legacyOps = new List<LegacyStockWriteOperation>();
        var adjustmentRecords = new List<StockOpnameAdjustmentModel>();
        var legacyDepletions = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var candidateAllocatedTracker = new Dictionary<string, decimal>(StringComparer.Ordinal);

        var executedAt = DateTime.Now;

        foreach (var item in itemsToAdjust)
        {
            Guard.Against.NullOrWhiteSpace(item.BrgId, nameof(item.BrgId));
            var executedBy = string.IsNullOrWhiteSpace(item.ExecutedBy) ? "SYSTEM" : item.ExecutedBy;
            var tglEd = DateTime.TryParse(item.ExpiredDate, out var parsedEd)
                ? parsedEd.Date
                : StockLedgerSentinel.EmptyDate;

            if (item.AdjustmentQty > 0)
            {
                // Surplus: AJ_PLUS (16)
                var candidates = _batchRepo.ListAllocationCandidates(item.BrgId, request.LayananId).ToList();
                var matchCandidate = candidates.FirstOrDefault(c =>
                    string.Equals(c.NoBatch, item.BatchNo, StringComparison.OrdinalIgnoreCase));

                var brgMasukReffId = matchCandidate?.BrgMasukReffId
                    ?? (!string.IsNullOrWhiteSpace(item.BatchNo) && item.BatchNo.Length <= 10
                        ? item.BatchNo
                        : NunaId.NewLegacyCompact("DO"));

                var batch = GetOrLoadBatch(item.BrgId, brgMasukReffId, item.Hpp, executedAt, batchesByNaturalKey);
                var lokasi = batch.IncreaseLokasi(request.LayananId, tglEd, item.AdjustmentQty, item.BatchNo);

                var legacyBukuId = NunaId.NewLegacyCompact("BK");
                var legacyStokId = NunaId.NewLegacyCompact("ST");

                var mutasi = StockMovementModel.CreateInbound(
                    lokasi.StokLokasiId,
                    batch.StokBatchId,
                    item.BrgId,
                    brgMasukReffId,
                    request.LayananId,
                    tglEd,
                    request.SessionId,
                    MovementKindEnum.AdjustmentPlus,
                    item.AdjustmentQty,
                    item.Hpp,
                    executedAt);

                mutasiInserts.Add(mutasi);

                var binding = StockLegacyBindingModel.CreateMutasiBuku(
                    mutasi.StokMutasiId,
                    legacyBukuId,
                    request.SessionId,
                    lokasi.StokLokasiId,
                    legacyStokId);

                bindingInserts.Add(binding);

                legacyOps.Add(new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    BrgId: item.BrgId,
                    BrgMasukReffId: brgMasukReffId,
                    LayananId: request.LayananId,
                    Qty: item.AdjustmentQty,
                    Hpp: item.Hpp,
                    TglEd: tglEd,
                    TglMasuk: executedAt,
                    TglMutasi: executedAt,
                    TrsReffId: request.SessionId,
                    MovementKindString: "AJ_PLUS",
                    NoBatch: item.BatchNo,
                    SatuanId: item.SatuanId,
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId)));

                var scopeModel = GetOrLoadScope(item.BrgId, brgMasukReffId, scopesByNaturalKey);
                scopeModel.MarkAligned(executedAt, legacyBukuId, executedAt);

                var adjModel = StockOpnameAdjustmentModel.Create(
                    adjustmentId: NunaId.New("ADJ"),
                    sessionId: request.SessionId,
                    brgId: item.BrgId,
                    batchNo: item.BatchNo ?? string.Empty,
                    expiredDate: item.ExpiredDate ?? string.Empty,
                    layananId: request.LayananId,
                    adjustmentQty: item.AdjustmentQty,
                    baseSatuanId: item.SatuanId ?? string.Empty,
                    hpp: item.Hpp,
                    stokMutasiId: mutasi.StokMutasiId,
                    legacyBukuId: legacyBukuId,
                    movementKind: (int)MovementKindEnum.AdjustmentPlus,
                    executedAt: executedAt,
                    executedBy: executedBy);

                adjustmentRecords.Add(adjModel);
            }
            else
            {
                // Deficit: AJ_MIN (18) with Tuple Preferred + FEFO Fallback (TD-10)
                var neededDeficit = Math.Abs(item.AdjustmentQty);
                var remainingDeficit = neededDeficit;

                var candidates = _batchRepo.ListAllocationCandidates(item.BrgId, request.LayananId).ToList();

                decimal GetAvailable(LocationStockBalanceModel c)
                {
                    var alreadyUsed = candidateAllocatedTracker.GetValueOrDefault(c.StokLokasiId, 0m);
                    return Math.Max(0m, c.QtySisa - alreadyUsed);
                }

                var allocations = new List<(LocationStockBalanceModel Candidate, decimal DeductQty)>();

                // 1. Preferred batch matching BatchNo and ExpiredDate
                var preferred = candidates.FirstOrDefault(c =>
                    string.Equals(c.NoBatch, item.BatchNo, StringComparison.OrdinalIgnoreCase)
                    && (tglEd == StockLedgerSentinel.EmptyDate || c.TglEd == tglEd)
                    && GetAvailable(c) > 0);

                if (preferred is null)
                {
                    preferred = candidates.FirstOrDefault(c =>
                        string.Equals(c.NoBatch, item.BatchNo, StringComparison.OrdinalIgnoreCase)
                        && GetAvailable(c) > 0);
                }

                if (preferred is not null)
                {
                    var avail = GetAvailable(preferred);
                    var take = Math.Min(remainingDeficit, avail);
                    allocations.Add((preferred, take));
                    candidateAllocatedTracker[preferred.StokLokasiId] =
                        candidateAllocatedTracker.GetValueOrDefault(preferred.StokLokasiId, 0m) + take;
                    remainingDeficit -= take;
                }

                // 2. FEFO fallback across other batches
                if (remainingDeficit > 0)
                {
                    var fallbackCandidates = candidates
                        .Where(c => (preferred is null || c.StokLokasiId != preferred.StokLokasiId) && GetAvailable(c) > 0)
                        .OrderBy(c => c.TglEd)
                        .ThenBy(c => c.TglMasuk)
                        .ToList();

                    foreach (var fb in fallbackCandidates)
                    {
                        if (remainingDeficit <= 0) break;
                        var avail = GetAvailable(fb);
                        var take = Math.Min(remainingDeficit, avail);
                        allocations.Add((fb, take));
                        candidateAllocatedTracker[fb.StokLokasiId] =
                            candidateAllocatedTracker.GetValueOrDefault(fb.StokLokasiId, 0m) + take;
                        remainingDeficit -= take;
                    }
                }

                if (remainingDeficit > 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for BrgId '{item.BrgId}' at location '{request.LayananId}' " +
                        $"to deduct deficit of {neededDeficit}. Shortfall: {remainingDeficit}.");
                }

                string primaryMutasiId = string.Empty;
                string primaryLegacyBukuId = string.Empty;

                foreach (var (cand, deductQty) in allocations)
                {
                    var batch = GetOrLoadBatch(item.BrgId, cand.BrgMasukReffId, item.Hpp, executedAt, batchesByNaturalKey);
                    batch.DecreaseLokasi(request.LayananId, cand.TglEd, deductQty);

                    // Deplete legacy balances
                    var legacyBalances = _legacyRead.ListBalances(item.BrgId, cand.BrgMasukReffId)
                        .Where(b => b.LayananId == request.LayananId && b.TglEd == cand.TglEd)
                        .Select(b =>
                        {
                            var alreadyDep = legacyDepletions.GetValueOrDefault(b.LegacyStokId, 0m);
                            return (b.LegacyStokId, QtySisa: b.QtySisa - alreadyDep);
                        })
                        .Where(b => b.QtySisa > 0)
                        .OrderBy(b => b.LegacyStokId, StringComparer.Ordinal)
                        .ToList();

                    var remainingToDeplete = deductQty;
                    var legacyStokIdForBinding = string.Empty;

                    foreach (var lb in legacyBalances)
                    {
                        if (remainingToDeplete <= 0) break;
                        var slice = Math.Min(remainingToDeplete, lb.QtySisa);
                        remainingToDeplete -= slice;
                        legacyDepletions[lb.LegacyStokId] =
                            legacyDepletions.GetValueOrDefault(lb.LegacyStokId, 0m) + slice;
                        legacyOps.Add(new LegacyStokDepleteWriteOperation(
                            new LegacyStokDepleteRequest(lb.LegacyStokId, slice)));
                        if (string.IsNullOrEmpty(legacyStokIdForBinding))
                            legacyStokIdForBinding = lb.LegacyStokId;
                    }

                    var legacyBukuId = NunaId.NewLegacyCompact("BK");
                    legacyOps.Add(new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                        BrgId: item.BrgId,
                        BrgMasukReffId: cand.BrgMasukReffId,
                        LayananId: request.LayananId,
                        QtyOut: deductQty,
                        Hpp: batch.Hpp,
                        TglEd: cand.TglEd,
                        TglMutasi: executedAt,
                        TrsReffId: request.SessionId,
                        MovementKindString: "AJ_MIN",
                        NoBatch: cand.NoBatch,
                        SatuanId: item.SatuanId,
                        LegacyBukuId: legacyBukuId)));

                    var mutasi = StockMovementModel.CreateOutbound(
                        cand.StokLokasiId,
                        batch.StokBatchId,
                        item.BrgId,
                        cand.BrgMasukReffId,
                        request.LayananId,
                        cand.TglEd,
                        request.SessionId,
                        MovementKindEnum.AdjustmentMinus,
                        deductQty,
                        batch.Hpp,
                        executedAt);

                    mutasiInserts.Add(mutasi);

                    var binding = StockLegacyBindingModel.CreateMutasiBuku(
                        mutasi.StokMutasiId,
                        legacyBukuId,
                        request.SessionId,
                        cand.StokLokasiId,
                        legacyStokIdForBinding);

                    bindingInserts.Add(binding);

                    var scopeModel = GetOrLoadScope(item.BrgId, cand.BrgMasukReffId, scopesByNaturalKey);
                    scopeModel.MarkAligned(executedAt, legacyBukuId, executedAt);

                    if (string.IsNullOrEmpty(primaryMutasiId))
                        primaryMutasiId = mutasi.StokMutasiId;
                    if (string.IsNullOrEmpty(primaryLegacyBukuId))
                        primaryLegacyBukuId = legacyBukuId;
                }

                var adjModel = StockOpnameAdjustmentModel.Create(
                    adjustmentId: NunaId.New("ADJ"),
                    sessionId: request.SessionId,
                    brgId: item.BrgId,
                    batchNo: item.BatchNo ?? string.Empty,
                    expiredDate: item.ExpiredDate ?? string.Empty,
                    layananId: request.LayananId,
                    adjustmentQty: item.AdjustmentQty,
                    baseSatuanId: item.SatuanId ?? string.Empty,
                    hpp: item.Hpp,
                    stokMutasiId: primaryMutasiId,
                    legacyBukuId: primaryLegacyBukuId,
                    movementKind: (int)MovementKindEnum.AdjustmentMinus,
                    executedAt: executedAt,
                    executedBy: executedBy);

                adjustmentRecords.Add(adjModel);
            }
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: batchesByNaturalKey.Values.Distinct().ToList(),
            MutasiInserts: mutasiInserts,
            BindingInserts: bindingInserts,
            ScopeUpdates: scopesByNaturalKey.Values.Distinct().ToList(),
            LegacyOperations: legacyOps,
            UserId: itemsToAdjust.FirstOrDefault()?.ExecutedBy ?? "SYSTEM");

        _uow.Commit(draft);
        _adjustmentRepo.Insert(adjustmentRecords);

        trans.Complete();

        return new PostStockAdjustmentConsequenceResult(
            PostStockAdjustmentOutcomeEnum.Success,
            request.SessionId,
            adjustmentRecords);
    }

    private StockBatchModel GetOrLoadBatch(
        string brgId,
        string brgMasukReffId,
        decimal hpp,
        DateTime now,
        Dictionary<(string BrgId, string DoId), StockBatchModel> cache)
    {
        if (cache.TryGetValue((brgId, brgMasukReffId), out var existing))
            return existing;

        var loaded = _batchRepo.LoadByNaturalKey(brgId, brgMasukReffId);
        var batch = loaded.HasValue
            ? loaded.Value
            : StockBatchModel.Create(brgId, brgMasukReffId, hpp, now);

        cache[(brgId, brgMasukReffId)] = batch;
        return batch;
    }

    private StockLegacyScopeModel GetOrLoadScope(
        string brgId,
        string brgMasukReffId,
        Dictionary<(string BrgId, string DoId), StockLegacyScopeModel> cache)
    {
        if (cache.TryGetValue((brgId, brgMasukReffId), out var existing))
            return existing;

        var key = StockLegacyScopeModel.Key(brgId, brgMasukReffId);
        var loaded = _scopeRepo.LoadEntity(key);
        var scope = loaded.HasValue
            ? loaded.Value
            : StockLegacyScopeModel.CreateNotAligned(brgId, brgMasukReffId);

        cache[(brgId, brgMasukReffId)] = scope;
        return scope;
    }
}
