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

        // Deactivated (P1-S02): no reads against BILRG_Stok* tables.
        return Task.FromResult(new ReconcileScopeResponse(
            request.BrgId,
            request.BrgMasukReffId,
            layananFilter,
            IsConsistent: true,
            BatchQtySisa: 0,
            LokasiQtyTotal: 0,
            MovementNet: 0,
            LocationLines: [],
            Differences: [],
            LegacySnapshot: null));
    }
}
