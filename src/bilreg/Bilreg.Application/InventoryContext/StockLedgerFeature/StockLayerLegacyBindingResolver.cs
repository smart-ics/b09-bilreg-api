using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — Resolves an exact LegacyRowId for a FIFO-selected Stock Layer.
/// Prefer durable binding; allow unique lazy establishment only when material attributes
/// identify exactly one surviving balance row. Never uses quantity as a tie-breaker.
/// </summary>
public sealed class StockLayerLegacyBindingResolver
{
    public enum OutcomeKind
    {
        Resolved = 1,
        Ambiguous = 2,
        Missing = 3
    }

    public sealed record Resolution(
        OutcomeKind Kind,
        string? LegacyRowId,
        StockLayerLegacyBindingType? BindingToPersist,
        string Explanation);

    private readonly IStockLayerLegacyBindingRepo _bindingRepo;

    public StockLayerLegacyBindingResolver(IStockLayerLegacyBindingRepo bindingRepo)
    {
        _bindingRepo = bindingRepo ?? throw new ArgumentNullException(nameof(bindingRepo));
    }

    /// <summary>
    /// Resolve the physical source row for one allocated layer against a live balance snapshot.
    /// <paramref name="claimedLegacyRowIds"/> prevents two allocations from claiming the same
    /// unbound row inside one consequence.
    /// </summary>
    public Resolution ResolveSourceRow(
        StockLayerModel sourceLayer,
        decimal requiredQuantity,
        IReadOnlyList<LegacyStockBalanceType> sourceBalances,
        ISet<string> claimedLegacyRowIds,
        DateTime boundAt)
    {
        ArgumentNullException.ThrowIfNull(sourceLayer);
        ArgumentNullException.ThrowIfNull(sourceBalances);
        ArgumentNullException.ThrowIfNull(claimedLegacyRowIds);
        if (requiredQuantity <= 0m)
        {
            return new Resolution(
                OutcomeKind.Missing,
                null,
                null,
                $"Allocated quantity for layer '{sourceLayer.StockLayerId}' must be positive.");
        }

        if (boundAt == default)
        {
            return new Resolution(
                OutcomeKind.Missing,
                null,
                null,
                "BoundAt is required for coexistence binding establishment.");
        }

        var existing = _bindingRepo.LoadByStockLayerId(sourceLayer.StockLayerId);
        if (existing.HasValue)
        {
            var binding = existing.Value;
            var boundRow = sourceBalances.FirstOrDefault(b =>
                !string.IsNullOrWhiteSpace(b.LegacyRowId)
                && string.Equals(b.LegacyRowId.Trim(), binding.LegacyRowId, StringComparison.Ordinal)
                && string.Equals(b.LayananId.Trim(), sourceLayer.LayananId, StringComparison.Ordinal));

            if (boundRow is null)
            {
                return new Resolution(
                    OutcomeKind.Missing,
                    null,
                    null,
                    $"Bound LegacyRowId '{binding.LegacyRowId}' for layer '{sourceLayer.StockLayerId}' "
                    + "is not present among surviving source balances; fail closed.");
            }

            if (!MaterialMatches(sourceLayer, boundRow))
            {
                return new Resolution(
                    OutcomeKind.Ambiguous,
                    null,
                    null,
                    $"Bound LegacyRowId '{binding.LegacyRowId}' for layer '{sourceLayer.StockLayerId}' "
                    + "no longer matches layer material evidence; fail closed as Inconsistent.");
            }

            if (boundRow.Quantity < requiredQuantity)
            {
                return new Resolution(
                    OutcomeKind.Missing,
                    null,
                    null,
                    $"Bound LegacyRowId '{binding.LegacyRowId}' has qty {boundRow.Quantity} "
                    + $"but allocation requires {requiredQuantity}.");
            }

            if (!claimedLegacyRowIds.Add(binding.LegacyRowId))
            {
                return new Resolution(
                    OutcomeKind.Ambiguous,
                    null,
                    null,
                    $"LegacyRowId '{binding.LegacyRowId}' was already claimed by another allocation slice.");
            }

            return new Resolution(
                OutcomeKind.Resolved,
                binding.LegacyRowId,
                BindingToPersist: null,
                $"Durable binding StockLayerId '{sourceLayer.StockLayerId}' -> LegacyRowId '{binding.LegacyRowId}'.");
        }

        // Lazy unique establishment: material attributes only — never quantity tie-break.
        var candidates = sourceBalances
            .Where(b =>
                !string.IsNullOrWhiteSpace(b.LegacyRowId)
                && !claimedLegacyRowIds.Contains(b.LegacyRowId!.Trim())
                && MaterialMatches(sourceLayer, b)
                && b.Quantity >= requiredQuantity)
            .Select(b => b.LegacyRowId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            return new Resolution(
                OutcomeKind.Missing,
                null,
                null,
                $"No surviving legacy balance uniquely associable to layer '{sourceLayer.StockLayerId}' "
                + $"at '{sourceLayer.LayananId}'.");
        }

        if (candidates.Count > 1)
        {
            return new Resolution(
                OutcomeKind.Ambiguous,
                null,
                null,
                $"Ambiguous legacy balance association for layer '{sourceLayer.StockLayerId}' "
                + $"at '{sourceLayer.LayananId}' ({candidates.Count} candidates); "
                + "coexistence binding required — fail closed.");
        }

        var legacyRowId = candidates[0];
        claimedLegacyRowIds.Add(legacyRowId);
        var bindingToPersist = StockLayerLegacyBindingType.FromLayer(
            sourceLayer,
            legacyRowId,
            boundAt);

        return new Resolution(
            OutcomeKind.Resolved,
            legacyRowId,
            bindingToPersist,
            $"Lazy unique binding StockLayerId '{sourceLayer.StockLayerId}' -> LegacyRowId '{legacyRowId}'.");
    }

    private static bool MaterialMatches(StockLayerModel layer, LegacyStockBalanceType balance)
        => string.Equals(balance.BrgId.Trim(), layer.BrgId, StringComparison.Ordinal)
           && string.Equals(balance.ReceiptSourceId.Trim(), layer.ReceiptSourceId, StringComparison.Ordinal)
           && string.Equals(balance.LayananId.Trim(), layer.LayananId, StringComparison.Ordinal)
           && balance.UnitCost == layer.UnitValuation.AmountPerUnit
           && Nullable.Equals(balance.ExpirationDate, layer.ExpirationDate)
           && string.Equals(
               NormalizeOptional(balance.Batch) ?? string.Empty,
               NormalizeOptional(layer.Batch) ?? string.Empty,
               StringComparison.Ordinal);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
