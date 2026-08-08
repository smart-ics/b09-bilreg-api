using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S3 / G-16 P0 — Pure material reconciliation classification (quantity compare only).
/// SQL-free and side-effect free; live adapter supplies legacy balances + Ledger layer snapshots.
/// Does not repair, mutate, or advance Synchronization Position.
/// </summary>
public static class StockReconciliationClassifier
{
    /// <summary>
    /// Lightweight Ledger layer projection for classification. Caller maps from persisted layers.
    /// </summary>
    public sealed record LedgerLayerSnapshot(
        string LayananId,
        decimal RemainingQuantity,
        bool IsDepleted);

    /// <summary>
    /// P3-S4 material-safety handoff: remaining quantities are consistent enough that catch-up
    /// may complete Synchronization Position after applying sync intents.
    /// <para>
    /// True for <see cref="StockReconciliationOutcomeEnum.Balanced"/>,
    /// <see cref="StockReconciliationOutcomeEnum.IntentionalDifference"/>, and
    /// <see cref="StockReconciliationOutcomeEnum.PendingSynchronization"/>
    /// (Port overlay when Scope is still SynchronizationRequired / LegacyChangePending —
    /// material already matched; lifecycle completion remains P3-S4 orchestration).
    /// </para>
    /// False for <see cref="StockReconciliationOutcomeEnum.MaterialInconsistency"/> and
    /// <see cref="StockReconciliationOutcomeEnum.ProvenanceLimitation"/>.
    /// </summary>
    /// <remarks>
    /// This is <b>material</b> reconciliation safety, not Scope lifecycle completion.
    /// Do not treat <see cref="StockReconciliationOutcomeEnum.PendingSynchronization"/> as a
    /// material failure — P3-S4 must reconcile before <c>CompleteSynchronization</c>.
    /// </remarks>
    public static bool AllowsMaterialSynchronizationAdvance(StockReconciliationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Outcome is StockReconciliationOutcomeEnum.Balanced
            or StockReconciliationOutcomeEnum.IntentionalDifference
            or StockReconciliationOutcomeEnum.PendingSynchronization;
    }

    public static StockReconciliationResult Classify(
        IStockLedgerScopeKey scope,
        IReadOnlyList<LegacyStockBalanceType> legacyBalances,
        IReadOnlyList<LedgerLayerSnapshot> ledgerLayers)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(legacyBalances);
        ArgumentNullException.ThrowIfNull(ledgerLayers);

        var legacyByLocation = legacyBalances
            .GroupBy(b => b.LayananId ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(b => b.Quantity),
                StringComparer.Ordinal);

        var layersByLocation = ledgerLayers
            .GroupBy(l => l.LayananId ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.ToList(),
                StringComparer.Ordinal);

        var locations = legacyByLocation.Keys
            .Union(layersByLocation.Keys, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var differences = new List<StockReconciliationDifferenceType>();
        decimal legacyTotal = 0m;
        decimal ledgerTotal = 0m;

        foreach (var layananId in locations)
        {
            legacyByLocation.TryGetValue(layananId, out var legacyQty);
            layersByLocation.TryGetValue(layananId, out var layersAtLocation);
            layersAtLocation ??= [];

            var ledgerQty = layersAtLocation.Sum(l => l.RemainingQuantity);
            var hasLegacyRow = legacyByLocation.ContainsKey(layananId);
            var hasLedgerLayers = layersAtLocation.Count > 0;
            var onlyDepletedLayers = hasLedgerLayers
                && layersAtLocation.All(l => l.IsDepleted || l.RemainingQuantity == 0m);

            legacyTotal += legacyQty;
            ledgerTotal += ledgerQty;

            if (legacyQty == ledgerQty)
            {
                // Intentional representational difference: depleted Ledger layers retained
                // while zero tb_stok rows are absent (BR-STL-080 / BR-STL-110).
                if (!hasLegacyRow
                    && legacyQty == 0m
                    && ledgerQty == 0m
                    && onlyDepletedLayers)
                {
                    differences.Add(new StockReconciliationDifferenceType(
                        StockReconciliationDifferenceKindEnum.DepletedLayerVsAbsentLegacyRow,
                        layananId,
                        "Absent tb_stok row with depleted Ledger layer(s) at Remaining Quantity 0 "
                        + "is intentional representational difference (BR-STL-080/110); not material drift."));
                }

                continue;
            }

            if (hasLegacyRow && legacyQty > 0m && !hasLedgerLayers)
            {
                differences.Add(new StockReconciliationDifferenceType(
                    StockReconciliationDifferenceKindEnum.MissingLedgerMovement,
                    layananId,
                    $"Legacy remaining {legacyQty} at location '{layananId}' has no Ledger layers."));
                continue;
            }

            differences.Add(new StockReconciliationDifferenceType(
                StockReconciliationDifferenceKindEnum.QuantityMismatch,
                layananId,
                $"Remaining quantity mismatch at location '{layananId}': "
                + $"legacy={legacyQty}, ledger={ledgerQty}."));
        }

        var differenceQuantity = ledgerTotal - legacyTotal;
        var materialDifferences = differences
            .Where(d => d.Kind is StockReconciliationDifferenceKindEnum.QuantityMismatch
                or StockReconciliationDifferenceKindEnum.MissingLedgerMovement
                or StockReconciliationDifferenceKindEnum.MissingLegacyJournal
                or StockReconciliationDifferenceKindEnum.ValuationMismatch
                or StockReconciliationDifferenceKindEnum.Other)
            .ToList();

        if (materialDifferences.Count > 0 || differenceQuantity != 0m)
        {
            return new StockReconciliationResult(
                StockReconciliationOutcomeEnum.MaterialInconsistency,
                legacyTotal,
                ledgerTotal,
                differenceQuantity,
                differences,
                "Material remaining-quantity mismatch between legacy authority and Stock Ledger; "
                + "not safe to advance Synchronization Position.");
        }

        var intentionalOnly = differences.Count > 0
            && differences.All(d =>
                d.Kind == StockReconciliationDifferenceKindEnum.DepletedLayerVsAbsentLegacyRow);

        if (intentionalOnly)
        {
            return new StockReconciliationResult(
                StockReconciliationOutcomeEnum.IntentionalDifference,
                legacyTotal,
                ledgerTotal,
                0m,
                differences,
                "Scope remaining quantities match; depleted-layer vs absent legacy row is intentional "
                + "(BR-STL-080/110). Safe to advance Synchronization Position.");
        }

        return new StockReconciliationResult(
            StockReconciliationOutcomeEnum.Balanced,
            legacyTotal,
            ledgerTotal,
            0m,
            Array.Empty<StockReconciliationDifferenceType>(),
            "Legacy remaining quantity matches Ledger Remaining Quantity across all locations.");
    }
}
