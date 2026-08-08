using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S4 — Loads an in-memory <see cref="LegacySyncLedgerSnapshot"/> for the pure interpreter.
/// Parses only material P3-S1 discovery identity keys; synthetic VOID/OMISSION keys are opaque (R-004).
/// </summary>
public sealed class LegacySyncLedgerSnapshotLoader
{
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockMovementRepo _movementRepo;

    public LegacySyncLedgerSnapshotLoader(
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockPositionRepo positionRepo,
        IStockMovementRepo movementRepo)
    {
        _idempotencyRepo = idempotencyRepo;
        _positionRepo = positionRepo;
        _movementRepo = movementRepo;
    }

    public LegacySyncLedgerSnapshot Load(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var records = _idempotencyRepo.ListSyncIdentityRecordsForScope(scope);
        var positions = _positionRepo.ListByLedgerScope(scope);
        var layers = positions.SelectMany(p => p.Layers).ToList();

        var reconstructionMovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(scope);
        var reconstructionMovement = _movementRepo.LoadEntity(
            StockMovementModel.Key(reconstructionMovementId));

        var journalAnchors = new Dictionary<LegacyJournalIdentity, LegacySyncJournalAnchor>();
        var balanceAnchors = new Dictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor>();
        var movementsById = new Dictionary<string, StockMovementModel>(StringComparer.Ordinal);

        if (reconstructionMovement.HasValue)
            movementsById[reconstructionMovement.Value.StockMovementId] = reconstructionMovement.Value;

        foreach (var record in records)
        {
            if (LegacyChangeDiscoveryIdentityKeys.TryParseJournalKey(
                    record.IdempotencyKey,
                    out var journalIdentity,
                    out _))
            {
                var movementId = string.IsNullOrWhiteSpace(record.StockMovementId)
                    ? (reconstructionMovement.HasValue ? reconstructionMovementId : string.Empty)
                    : record.StockMovementId.Trim();

                if (string.IsNullOrWhiteSpace(movementId))
                    continue;

                if (!movementsById.TryGetValue(movementId, out var movement))
                {
                    var loaded = _movementRepo.LoadEntity(StockMovementModel.Key(movementId));
                    if (loaded.HasValue)
                    {
                        movement = loaded.Value;
                        movementsById[movementId] = movement;
                    }
                }

                journalAnchors[journalIdentity] = new LegacySyncJournalAnchor(
                    movementId,
                    movementsById.GetValueOrDefault(movementId));
                continue;
            }

            if (LegacyChangeDiscoveryIdentityKeys.TryParseBalanceKey(
                    record.IdempotencyKey,
                    out var balanceIdentity,
                    out var balanceMaterial))
            {
                var layer = MatchLayer(layers, balanceIdentity, balanceMaterial);
                if (layer is null)
                    continue;

                balanceAnchors[balanceIdentity] = new LegacySyncBalanceAnchor(
                    layer.StockLayerId,
                    layer.RemainingQuantity,
                    layer.Origin,
                    layer.UnitValuation.AmountPerUnit,
                    layer.ExpirationDate,
                    layer.Batch);
            }

            // Synthetic VOID / OMISSION keys: TryParse* fails — ignored for anchors (R-004).
        }

        return LegacySyncLedgerSnapshot.Create(
            journalAnchors,
            balanceAnchors,
            movementsById.Values.ToArray());
    }

    private static StockLayerModel? MatchLayer(
        IReadOnlyList<StockLayerModel> layers,
        LegacyBalanceIdentity identity,
        LegacyBalanceMaterialSnapshot material)
    {
        var candidates = layers
            .Where(l => string.Equals(l.LayananId, identity.LayananId, StringComparison.Ordinal))
            .Where(l => l.UnitValuation.AmountPerUnit == material.UnitCost)
            .Where(l => l.ExpirationDate == material.ExpirationDate)
            .Where(l => string.Equals(l.Batch ?? string.Empty, material.Batch ?? string.Empty, StringComparison.Ordinal))
            .Where(l => l.RemainingQuantity == material.Quantity)
            .ToList();

        if (candidates.Count == 1)
            return candidates[0];

        // Quantity may have drifted after bootstrap before catch-up; match without remaining qty.
        candidates = layers
            .Where(l => string.Equals(l.LayananId, identity.LayananId, StringComparison.Ordinal))
            .Where(l => l.UnitValuation.AmountPerUnit == material.UnitCost)
            .Where(l => l.ExpirationDate == material.ExpirationDate)
            .Where(l => string.Equals(l.Batch ?? string.Empty, material.Batch ?? string.Empty, StringComparison.Ordinal))
            .ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }
}
