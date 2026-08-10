using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-020 — Reconcile Scope (read-only). Includes depleted balances (BR-STL-032).
/// Differences are reported explicitly; never silently repaired (BR-STL-035).
/// </summary>
public record ReconcileScopeQuery(
    string BrgId,
    string BrgMasukReffId,
    string? LayananId = null) : IRequest<ReconcileScopeResponse>;

public enum ReconcileDifferenceKindEnum
{
    HospitalBatchVsLokasi = 0,
    HospitalBatchVsMovement = 1,
    LokasiVsMovement = 2,
    LegacyVsLedger = 3,
    ScopedLokasiVsMovement = 4
}

public record ReconcileDifference(
    ReconcileDifferenceKindEnum Kind,
    string Message,
    decimal Expected,
    decimal Actual,
    string? StokLokasiId = null);

public record ReconcileLocationLine(
    string StokLokasiId,
    string LayananId,
    DateTime TglEd,
    decimal QtySisa,
    decimal MovementNet,
    bool IsDepleted);

public record LegacyReconcileSnapshot(
    decimal LegacyQtyTotal,
    decimal LedgerLokasiTotal,
    int LegacyBalanceCount);

public record ReconcileScopeResponse(
    string BrgId,
    string BrgMasukReffId,
    string? LayananId,
    bool IsConsistent,
    decimal BatchQtySisa,
    decimal LokasiQtyTotal,
    decimal MovementNet,
    IReadOnlyList<ReconcileLocationLine> LocationLines,
    IReadOnlyList<ReconcileDifference> Differences,
    LegacyReconcileSnapshot? LegacySnapshot);

public class ReconcileScopeHandler : IRequestHandler<ReconcileScopeQuery, ReconcileScopeResponse>
{
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly StockLedgerCoexistenceOptions _coexistence;

    public ReconcileScopeHandler(
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        ILegacyStockReadPort legacyRead,
        IOptions<StockLedgerCoexistenceOptions> coexistence)
    {
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _legacyRead = legacyRead;
        _coexistence = coexistence.Value;
    }

    public Task<ReconcileScopeResponse> Handle(
        ReconcileScopeQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);

        var layananFilter = string.IsNullOrWhiteSpace(request.LayananId)
            ? null
            : request.LayananId;

        var batchMaybe = _batchRepo.LoadByNaturalKey(request.BrgId, request.BrgMasukReffId);
        var mutasi = _mutasiRepo
            .ListByScope(request.BrgId, request.BrgMasukReffId, layananFilter)
            .ToList();

        List<LocationStockBalanceModel> lokasi;
        decimal batchQty;
        if (batchMaybe.HasValue)
        {
            var batch = batchMaybe.Value;
            batchQty = batch.QtySisa;
            lokasi = batch.ListLokasi
                .Where(x => layananFilter is null || x.LayananId == layananFilter)
                .ToList();
        }
        else
        {
            batchQty = 0;
            // Batch missing: synthesize zero-qty lines from mutasi keys (BR-STL-032 accountability).
            lokasi = mutasi
                .GroupBy(x => x.StokLokasiId)
                .Select(g =>
                {
                    var sample = g.First();
                    return new LocationStockBalanceModel(
                        sample.StokLokasiId,
                        sample.StokBatchId,
                        sample.BrgId,
                        sample.BrgMasukReffId,
                        sample.LayananId,
                        sample.TglEd,
                        string.Empty,
                        StockLedgerSentinel.EmptyDate,
                        qtySisa: 0,
                        version: 0);
                })
                .ToList();
        }

        var differences = new List<ReconcileDifference>();
        var mutasiByLokasi = mutasi
            .GroupBy(x => x.StokLokasiId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QtyIn - x.QtyOut));

        var locationLines = new List<ReconcileLocationLine>();
        foreach (var loc in lokasi)
        {
            var movementNetLokasi = mutasiByLokasi.GetValueOrDefault(loc.StokLokasiId, 0m);
            locationLines.Add(new ReconcileLocationLine(
                loc.StokLokasiId,
                loc.LayananId,
                loc.TglEd,
                loc.QtySisa,
                movementNetLokasi,
                IsDepleted: loc.QtySisa == 0));

            if (loc.QtySisa != movementNetLokasi)
            {
                differences.Add(new ReconcileDifference(
                    ReconcileDifferenceKindEnum.LokasiVsMovement,
                    $"Location {loc.StokLokasiId} QtySisa={loc.QtySisa} != movement net={movementNetLokasi}.",
                    Expected: loc.QtySisa,
                    Actual: movementNetLokasi,
                    StokLokasiId: loc.StokLokasiId));
            }
        }

        // Mutasi for lokasi not present on batch (orphans).
        foreach (var orphan in mutasiByLokasi.Keys.Except(lokasi.Select(x => x.StokLokasiId)))
        {
            var movementNetLokasi = mutasiByLokasi[orphan];
            locationLines.Add(new ReconcileLocationLine(
                orphan,
                mutasi.First(x => x.StokLokasiId == orphan).LayananId,
                mutasi.First(x => x.StokLokasiId == orphan).TglEd,
                QtySisa: 0,
                movementNetLokasi,
                IsDepleted: true));

            if (movementNetLokasi != 0)
            {
                differences.Add(new ReconcileDifference(
                    ReconcileDifferenceKindEnum.LokasiVsMovement,
                    $"Orphan mutasi for StokLokasiId={orphan}: movement net={movementNetLokasi} with no balance row.",
                    Expected: 0,
                    Actual: movementNetLokasi,
                    StokLokasiId: orphan));
            }
        }

        var lokasiTotal = lokasi.Sum(x => x.QtySisa);
        var movementNet = mutasi.Sum(x => x.QtyIn - x.QtyOut);

        if (layananFilter is null)
        {
            // Hospital-wide conservation (BR-STL-005 / BR-STL-030).
            if (batchQty != lokasiTotal)
            {
                differences.Add(new ReconcileDifference(
                    ReconcileDifferenceKindEnum.HospitalBatchVsLokasi,
                    $"Batch QtySisa={batchQty} != Σ lokasi QtySisa={lokasiTotal}.",
                    Expected: batchQty,
                    Actual: lokasiTotal));
            }

            if (mutasi.Count > 0 && batchQty != movementNet)
            {
                differences.Add(new ReconcileDifference(
                    ReconcileDifferenceKindEnum.HospitalBatchVsMovement,
                    $"Batch QtySisa={batchQty} != Σ movement net={movementNet}.",
                    Expected: batchQty,
                    Actual: movementNet));
            }
        }
        else if (lokasiTotal != movementNet)
        {
            differences.Add(new ReconcileDifference(
                ReconcileDifferenceKindEnum.ScopedLokasiVsMovement,
                $"Scoped LayananId={layananFilter}: Σ lokasi={lokasiTotal} != Σ movement net={movementNet}.",
                Expected: lokasiTotal,
                Actual: movementNet));
        }

        LegacyReconcileSnapshot? legacySnapshot = null;
        if (_coexistence.CoexistenceEnabled)
        {
            var legacyBalances = _legacyRead
                .ListBalances(request.BrgId, request.BrgMasukReffId)
                .Where(x => layananFilter is null || x.LayananId == layananFilter)
                .ToList();
            var legacyTotal = legacyBalances.Sum(x => x.QtySisa);
            legacySnapshot = new LegacyReconcileSnapshot(
                legacyTotal,
                lokasiTotal,
                legacyBalances.Count);

            if (legacyTotal != lokasiTotal)
            {
                differences.Add(new ReconcileDifference(
                    ReconcileDifferenceKindEnum.LegacyVsLedger,
                    $"Legacy Σ qty={legacyTotal} != ledger Σ lokasi={lokasiTotal}.",
                    Expected: lokasiTotal,
                    Actual: legacyTotal));
            }
        }

        var response = new ReconcileScopeResponse(
            request.BrgId,
            request.BrgMasukReffId,
            layananFilter,
            IsConsistent: differences.Count == 0,
            batchQty,
            lokasiTotal,
            movementNet,
            locationLines,
            differences,
            legacySnapshot);

        return Task.FromResult(response);
    }
}
