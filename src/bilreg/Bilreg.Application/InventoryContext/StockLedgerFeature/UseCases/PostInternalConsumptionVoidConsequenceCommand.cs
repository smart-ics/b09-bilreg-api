using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-007 — Post Internal Consumption Void Consequence (reverse-journal via Binding; never delete buku).
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
    private const MovementKindEnum ConsumptionKind = MovementKindEnum.InternalConsumption;
    private const MovementKindEnum VoidKind = MovementKindEnum.InternalConsumptionVoid;
    private const string VoidLegacyKindString = "PK_V";

    private readonly LegacyFreshnessGate _freshnessGate;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockConsequenceUnitOfWork _uow;

    public PostInternalConsumptionVoidConsequenceHandler(
        LegacyFreshnessGate freshnessGate,
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo,
        ILegacyStockReadPort legacyRead,
        IStockConsequenceUnitOfWork uow)
    {
        _freshnessGate = freshnessGate;
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
        _legacyRead = legacyRead;
        _uow = uow;
    }

    public async Task<PostInternalConsumptionVoidConsequenceResult> Handle(
        PostInternalConsumptionVoidConsequenceCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OriginalTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.VoidTrsReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var priorVoid = _mutasiRepo.ListByTrsReffId(request.VoidTrsReffId)
            .Where(x => x.MovementKind == VoidKind)
            .ToList();

        if (priorVoid.Count > 0)
        {
            ValidateIdempotentReplay(request, priorVoid);
            return BuildIdempotentResult(priorVoid);
        }

        var originals = _mutasiRepo.ListByTrsReffId(request.OriginalTrsReffId)
            .Where(x => x.MovementKind == ConsumptionKind)
            .ToList();

        if (originals.Count == 0)
        {
            return new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.OriginalNotFound,
                [],
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        if (originals.Any(x => x.QtyOut <= 0 || x.QtyIn != 0))
        {
            throw new InvalidOperationException(
                $"OriginalTrsReffId={request.OriginalTrsReffId} has non-outbound internal-consumption lines.");
        }

        var bindingsByMutasi = new Dictionary<string, StockLegacyBindingModel>(StringComparer.Ordinal);
        foreach (var original in originals)
        {
            var bindingMaybe = _bindingRepo.FindByStokMutasiId(original.StokMutasiId);
            if (!bindingMaybe.HasValue)
            {
                return new PostInternalConsumptionVoidConsequenceResult(
                    PostInternalConsumptionVoidOutcomeEnum.Unbound,
                    [],
                    original.StokMutasiId,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }

            bindingsByMutasi[original.StokMutasiId] = bindingMaybe.Value;

            if (_mutasiRepo.ExistsReversalFor(original.StokMutasiId))
            {
                return new PostInternalConsumptionVoidConsequenceResult(
                    PostInternalConsumptionVoidOutcomeEnum.AlreadyReversed,
                    [],
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }
        }

        var scopeKeys = originals
            .Select(x => new ScopeKeyDto(x.BrgId, x.BrgMasukReffId))
            .Distinct()
            .ToList();

        var freshness = await _freshnessGate.EnsureFreshAsync(
            scopeKeys,
            request.UserId,
            cancellationToken);

        if (freshness.Outcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent)
        {
            return new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.AbortedInconsistent,
                [],
                string.Empty,
                freshness.InconsistencyReason,
                freshness.FailedBrgId,
                freshness.FailedBrgMasukReffId);
        }

        var batchesByKey = new Dictionary<string, StockBatchModel>(StringComparer.Ordinal);
        var scopesByKey = new Dictionary<string, StockLegacyScopeModel>(StringComparer.Ordinal);
        var mutasiInserts = new List<StockMovementModel>();
        var bindingInserts = new List<StockLegacyBindingModel>();
        var legacyOps = new List<LegacyStockWriteOperation>();
        var lineResults = new List<PostInternalConsumptionVoidLineResult>();
        var syncedAt = DateTime.Now;

        foreach (var original in originals)
        {
            var binding = bindingsByMutasi[original.StokMutasiId];
            var key = $"{original.BrgId}|{original.BrgMasukReffId}";

            if (!batchesByKey.TryGetValue(key, out var batch))
            {
                var batchMaybe = _batchRepo.LoadByNaturalKey(original.BrgId, original.BrgMasukReffId);
                if (!batchMaybe.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Stock Batch not found for BrgId={original.BrgId}, BrgMasukReffId={original.BrgMasukReffId}.");
                }

                batch = batchMaybe.Value;
                batchesByKey[key] = batch;
            }

            var lokasi = batch.IncreaseLokasi(
                original.LayananId,
                original.TglEd,
                original.QtyOut);

            var noBatch = lokasi.NoBatch;
            var tglMasuk = lokasi.TglMasuk;
            var hpp = original.Hpp;

            var restoreStokId = ResolveRestoreLegacyStokId(
                original.BrgId,
                original.BrgMasukReffId,
                binding.LegacyStokId);

            var voidLegacyBukuId = NunaId.NewLegacyCompact("BK");

            var voidMutasi = StockMovementModel.CreateInbound(
                lokasi.StokLokasiId,
                batch.StokBatchId,
                original.BrgId,
                original.BrgMasukReffId,
                original.LayananId,
                original.TglEd,
                request.VoidTrsReffId,
                VoidKind,
                qtyIn: original.QtyOut,
                hpp: hpp,
                tglMutasi: request.TglMutasi,
                poReffId: original.PoReffId,
                reversesMutasiId: original.StokMutasiId);

            mutasiInserts.Add(voidMutasi);

            bindingInserts.Add(StockLegacyBindingModel.CreateMutasiBuku(
                voidMutasi.StokMutasiId,
                voidLegacyBukuId,
                request.VoidTrsReffId,
                lokasi.StokLokasiId,
                restoreStokId));

            legacyOps.Add(new LegacyReverseBukuWriteOperation(new LegacyReverseBukuWriteRequest(
                original.BrgId,
                original.BrgMasukReffId,
                original.LayananId,
                QtyIn: original.QtyOut,
                QtyOut: 0,
                Hpp: hpp,
                TglEd: original.TglEd,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.VoidTrsReffId,
                MovementKindString: VoidLegacyKindString,
                PoReffId: original.PoReffId,
                NoBatch: noBatch,
                LegacyBukuId: voidLegacyBukuId)));

            legacyOps.Add(new LegacyStokRestoreWriteOperation(new LegacyStokRestoreRequest(
                PreferredLegacyStokId: binding.LegacyStokId,
                QtyIn: original.QtyOut,
                BrgId: original.BrgId,
                BrgMasukReffId: original.BrgMasukReffId,
                LayananId: original.LayananId,
                Hpp: hpp,
                TglEd: original.TglEd,
                TglMasuk: tglMasuk,
                TglMutasi: request.TglMutasi,
                TrsReffId: request.VoidTrsReffId,
                PoReffId: original.PoReffId,
                NoBatch: noBatch,
                LegacyStokIdIfRecreate: restoreStokId)));

            AdvanceScopeWatermark(
                scopesByKey,
                original.BrgId,
                original.BrgMasukReffId,
                request.TglMutasi,
                voidLegacyBukuId,
                syncedAt);

            lineResults.Add(new PostInternalConsumptionVoidLineResult(
                voidMutasi.StokMutasiId,
                original.StokMutasiId,
                lokasi.StokLokasiId,
                batch.StokBatchId,
                original.BrgMasukReffId,
                voidLegacyBukuId,
                restoreStokId,
                original.QtyOut));
        }

        var draft = new StockConsequenceDraft(
            BatchUpserts: batchesByKey.Values.ToList(),
            MutasiInserts: mutasiInserts,
            BindingInserts: bindingInserts,
            ScopeUpdates: scopesByKey.Values.ToList(),
            LegacyOperations: legacyOps,
            UserId: request.UserId);

        _uow.Commit(draft);

        return new PostInternalConsumptionVoidConsequenceResult(
            PostInternalConsumptionVoidOutcomeEnum.Success,
            lineResults,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private string ResolveRestoreLegacyStokId(
        string brgId,
        string brgMasukReffId,
        string preferredLegacyStokId)
    {
        var stillExists = _legacyRead.ListBalances(brgId, brgMasukReffId)
            .Any(b => string.Equals(b.LegacyStokId, preferredLegacyStokId, StringComparison.Ordinal));

        return stillExists
            ? preferredLegacyStokId
            : NunaId.NewLegacyCompact("ST");
    }

    private void ValidateIdempotentReplay(
        PostInternalConsumptionVoidConsequenceCommand request,
        IReadOnlyList<StockMovementModel> priorVoid)
    {
        var originalIds = _mutasiRepo.ListByTrsReffId(request.OriginalTrsReffId)
            .Where(x => x.MovementKind == ConsumptionKind)
            .Select(x => x.StokMutasiId)
            .ToHashSet(StringComparer.Ordinal);

        var reverseIds = priorVoid
            .Select(x => x.ReversesMutasiId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        if (originalIds.Count == 0
            || originalIds.Count != reverseIds.Count
            || !originalIds.SetEquals(reverseIds))
        {
            throw new InvalidOperationException(
                $"VoidTrsReffId={request.VoidTrsReffId} already has internal-consumption-void movements " +
                $"that do not reverse OriginalTrsReffId={request.OriginalTrsReffId}.");
        }
    }

    private PostInternalConsumptionVoidConsequenceResult BuildIdempotentResult(
        IReadOnlyList<StockMovementModel> priorVoid)
    {
        var results = new List<PostInternalConsumptionVoidLineResult>();

        foreach (var mutasi in priorVoid)
        {
            var binding = _bindingRepo.FindByStokMutasiId(mutasi.StokMutasiId);
            results.Add(new PostInternalConsumptionVoidLineResult(
                mutasi.StokMutasiId,
                mutasi.ReversesMutasiId,
                mutasi.StokLokasiId,
                mutasi.StokBatchId,
                mutasi.BrgMasukReffId,
                binding.HasValue ? binding.Value.LegacyBukuId : string.Empty,
                binding.HasValue ? binding.Value.LegacyStokId : string.Empty,
                mutasi.QtyIn));
        }

        return new PostInternalConsumptionVoidConsequenceResult(
            PostInternalConsumptionVoidOutcomeEnum.Idempotent,
            results,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private void AdvanceScopeWatermark(
        Dictionary<string, StockLegacyScopeModel> scopesByKey,
        string brgId,
        string brgMasukReffId,
        DateTime tglMutasi,
        string legacyBukuId,
        DateTime syncedAt)
    {
        var key = $"{brgId}|{brgMasukReffId}";
        if (!scopesByKey.TryGetValue(key, out var scope))
        {
            var scopeKey = StockLegacyScopeModel.Key(brgId, brgMasukReffId);
            var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
            scope = scopeMaybe.HasValue
                ? scopeMaybe.Value
                : StockLegacyScopeModel.CreateNotAligned(brgId, brgMasukReffId);
            scopesByKey[key] = scope;
        }

        var candidateAfter = LegacyWatermarkHelper.IsAfterWatermark(
            tglMutasi, legacyBukuId, scope.TglMutasiLast, scope.LastLegacyBukuId);

        if (scope.AlignmentStatus is AlignmentStatusEnum.Aligned or AlignmentStatusEnum.Stale)
        {
            if (candidateAfter)
                scope.AdvanceWatermark(tglMutasi, legacyBukuId, syncedAt);
            else
                scope.AdvanceWatermark(scope.TglMutasiLast, scope.LastLegacyBukuId, syncedAt);
        }
        else
        {
            scope.MarkAligned(tglMutasi, legacyBukuId, syncedAt);
        }
    }
}
