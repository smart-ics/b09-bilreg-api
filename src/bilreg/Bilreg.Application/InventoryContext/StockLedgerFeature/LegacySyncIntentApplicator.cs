using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S4 / R-003 — Mechanical projection of interpreter intents onto Position aggregates.
/// Does not reinterpret delta meaning; applies ProposedMovement / TargetLayerId fields as-is.
/// </summary>
public static class LegacySyncIntentApplicator
{
    public sealed record ApplicationResult(
        StockMovementModel? Movement,
        IReadOnlyList<StockPositionModel> Positions);

    /// <summary>
    /// Projects one intent onto the working position map. Mutates <paramref name="positionsByLocation"/>
    /// in place so subsequent intents see prior quantity effects within the same catch-up batch.
    /// </summary>
    public static ApplicationResult Apply(
        LegacySyncIntentType intent,
        IStockLedgerScopeKey scope,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(positionsByLocation);

        return intent.Kind switch
        {
            LegacySyncIntentKindEnum.RepresentationalBalanceOmission
                => new ApplicationResult(Movement: null, Positions: Array.Empty<StockPositionModel>()),

            LegacySyncIntentKindEnum.ReversePriorMovement
                or LegacySyncIntentKindEnum.CorrectPriorMovement
                => ApplyProposedMovement(intent, scope, positionsByLocation),

            LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt
                => ApplyReceiptEstablishment(intent, scope, positionsByLocation),

            LegacySyncIntentKindEnum.ApplyLegacySynchronizedOutbound
                => ApplyOutboundAllocation(intent, scope, positionsByLocation),

            LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity
                => ApplyLayerAdjustment(intent, scope, positionsByLocation),

            _ => throw new InvalidOperationException(
                $"Unsupported sync intent kind '{intent.Kind}'.")
        };
    }

    private static ApplicationResult ApplyProposedMovement(
        LegacySyncIntentType intent,
        IStockLedgerScopeKey scope,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        var movement = RequireMovement(intent);
        var touched = new List<StockPositionModel>();

        foreach (var line in movement.Lines)
        {
            var position = GetOrCreatePosition(scope, line.LayananId, positionsByLocation);
            if (string.IsNullOrWhiteSpace(line.StockLayerId))
            {
                if (line.Direction == StockMovementDirectionEnum.Inbound)
                {
                    var layer = StockLayerModel.Create(
                        BrgObatType.Key(scope.BrgId),
                        ReceiptSourceType.Create(scope.ReceiptSourceId),
                        LayananType.Key(line.LayananId),
                        movement,
                        line.Quantity,
                        line.UnitValuation,
                        movement.EffectiveBusinessTime,
                        StockFactOriginEnum.LegacySynchronized,
                        expirationDate: null,
                        batch: null);
                    position = position.AddLayer(layer);
                    positionsByLocation[line.LayananId] = position;
                    touched.Add(position);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Sync intent '{intent.Kind}' line {line.LineNo} requires StockLayerId for outbound effects.");
            }

            position = AdjustLayerRemaining(
                position,
                line.StockLayerId,
                line.Direction,
                line.Quantity);
            positionsByLocation[line.LayananId] = position;
            touched.Add(position);
        }

        return new ApplicationResult(movement, DeduplicatePositions(touched));
    }

    private static ApplicationResult ApplyReceiptEstablishment(
        LegacySyncIntentType intent,
        IStockLedgerScopeKey scope,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        var movement = RequireMovement(intent);
        var touched = new List<StockPositionModel>();
        var lines = new List<StockMovementLineType>();

        foreach (var line in movement.Lines)
        {
            if (line.Direction != StockMovementDirectionEnum.Inbound)
                throw new InvalidOperationException("Receipt establishment requires inbound lines.");

            var layerId = string.IsNullOrWhiteSpace(line.StockLayerId)
                ? LegacyReconstructionBaselineCalculator.DeterministicAccountableId(
                    $"SYNC|LYR|{scope.BrgId}|{scope.ReceiptSourceId}|{line.LayananId}|{movement.StockMovementId}|{line.LineNo}")
                : line.StockLayerId;

            var layer = StockLayerModel.Create(
                BrgObatType.Key(scope.BrgId),
                ReceiptSourceType.Create(scope.ReceiptSourceId),
                LayananType.Key(line.LayananId),
                movement,
                line.Quantity,
                line.UnitValuation,
                movement.EffectiveBusinessTime,
                StockFactOriginEnum.LegacySynchronized,
                expirationDate: null,
                batch: null,
                stockLayerId: layerId);

            var position = GetOrCreatePosition(scope, line.LayananId, positionsByLocation)
                .AddLayer(layer);
            positionsByLocation[line.LayananId] = position;
            touched.Add(position);
            lines.Add(line.WithStockLayer(StockLayerModel.Key(layerId)));
        }

        var linked = StockMovementModel.Rehydrate(
            movement.StockMovementId,
            movement.SourceTransaction,
            movement.MovementKind,
            movement.EffectiveBusinessTime,
            movement.Origin,
            lines,
            movement.ReversedMovementId,
            movement.CorrectedMovementId);

        return new ApplicationResult(linked, DeduplicatePositions(touched));
    }

    private static ApplicationResult ApplyOutboundAllocation(
        LegacySyncIntentType intent,
        IStockLedgerScopeKey scope,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        var movement = RequireMovement(intent);
        var touched = new List<StockPositionModel>();
        var lines = new List<StockMovementLineType>();
        var lineNo = 0;

        foreach (var line in movement.Lines)
        {
            if (line.Direction != StockMovementDirectionEnum.Outbound)
                throw new InvalidOperationException("Outbound sync intent requires outbound lines.");

            if (!string.IsNullOrWhiteSpace(line.StockLayerId))
            {
                var targeted = GetOrCreatePosition(scope, line.LayananId, positionsByLocation);
                targeted = AdjustLayerRemaining(
                    targeted,
                    line.StockLayerId,
                    StockMovementDirectionEnum.Outbound,
                    line.Quantity);
                positionsByLocation[line.LayananId] = targeted;
                touched.Add(targeted);
                lines.Add(line);
                continue;
            }

            var position = GetOrCreatePosition(scope, line.LayananId, positionsByLocation);
            var (allocation, next) = position.Allocate(line.Quantity);
            if (!allocation.IsFulfilled)
            {
                throw new InvalidOperationException(
                    $"Insufficient Ledger quantity at '{line.LayananId}' to apply synchronized outbound {line.Quantity}.");
            }

            positionsByLocation[line.LayananId] = next;
            touched.Add(next);

            foreach (var take in allocation.Allocations)
            {
                lineNo++;
                lines.Add(StockMovementLineType.Create(
                    lineNo,
                    BrgObatType.Key(scope.BrgId),
                    ReceiptSourceType.Create(scope.ReceiptSourceId),
                    LayananType.Key(line.LayananId),
                    StockMovementDirectionEnum.Outbound,
                    take.Quantity,
                    line.UnitValuation,
                    StockFactOriginEnum.LegacySynchronized,
                    StockLayerModel.Key(take.StockLayerId)));
            }
        }

        var linked = StockMovementModel.Rehydrate(
            movement.StockMovementId,
            movement.SourceTransaction,
            movement.MovementKind,
            movement.EffectiveBusinessTime,
            movement.Origin,
            lines.Count == 0 ? movement.Lines : lines,
            movement.ReversedMovementId,
            movement.CorrectedMovementId);

        return new ApplicationResult(linked, DeduplicatePositions(touched));
    }

    private static ApplicationResult ApplyLayerAdjustment(
        LegacySyncIntentType intent,
        IStockLedgerScopeKey scope,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        var movement = RequireMovement(intent);
        if (string.IsNullOrWhiteSpace(intent.TargetLayerId))
            throw new InvalidOperationException("AdjustLayerRemainingQuantity requires TargetLayerId.");

        var layananId = intent.LayananId
            ?? movement.Lines.FirstOrDefault()?.LayananId
            ?? throw new InvalidOperationException("AdjustLayerRemainingQuantity is missing LayananId.");

        var position = GetOrCreatePosition(scope, layananId, positionsByLocation);
        foreach (var line in movement.Lines)
        {
            var layerId = line.StockLayerId ?? intent.TargetLayerId;
            position = AdjustLayerRemaining(position, layerId, line.Direction, line.Quantity);
        }

        positionsByLocation[layananId] = position;
        return new ApplicationResult(movement, [position]);
    }

    private static StockPositionModel AdjustLayerRemaining(
        StockPositionModel position,
        string stockLayerId,
        StockMovementDirectionEnum direction,
        decimal quantity)
    {
        var layers = new List<StockLayerModel>();
        var found = false;
        foreach (var layer in position.Layers)
        {
            if (!string.Equals(layer.StockLayerId, stockLayerId, StringComparison.Ordinal))
            {
                layers.Add(layer);
                continue;
            }

            found = true;
            // Preserve establishment origin (R-003) — only RemainingQuantity changes.
            var nextRemaining = direction == StockMovementDirectionEnum.Outbound
                ? layer.RemainingQuantity - quantity
                : layer.RemainingQuantity + quantity;

            if (nextRemaining < 0m)
                throw new InvalidOperationException(
                    $"Cannot adjust layer '{stockLayerId}' below zero (remaining {layer.RemainingQuantity}, delta {direction} {quantity}).");

            if (nextRemaining > layer.InitialQuantity)
            {
                // Inbound adjustment beyond initial: raise InitialQuantity to retain invariant.
                layers.Add(StockLayerModel.Rehydrate(
                    layer.StockLayerId,
                    layer.BrgId,
                    layer.ReceiptSourceId,
                    layer.LayananId,
                    layer.LayerFormingMovementId,
                    initialQuantity: nextRemaining,
                    remainingQuantity: nextRemaining,
                    layer.UnitValuation,
                    layer.EffectiveReceiptTime,
                    layer.Origin,
                    layer.ExpirationDate,
                    layer.Batch));
            }
            else if (direction == StockMovementDirectionEnum.Outbound)
            {
                layers.Add(layer.Consume(quantity));
            }
            else
            {
                layers.Add(StockLayerModel.Rehydrate(
                    layer.StockLayerId,
                    layer.BrgId,
                    layer.ReceiptSourceId,
                    layer.LayananId,
                    layer.LayerFormingMovementId,
                    layer.InitialQuantity,
                    nextRemaining,
                    layer.UnitValuation,
                    layer.EffectiveReceiptTime,
                    layer.Origin,
                    layer.ExpirationDate,
                    layer.Batch));
            }
        }

        if (!found)
            throw new InvalidOperationException(
                $"Target layer '{stockLayerId}' was not found in position '{position.LayananId}'.");

        return StockPositionModel.Create(position, layers, position.Version + 1);
    }

    private static StockPositionModel GetOrCreatePosition(
        IStockLedgerScopeKey scope,
        string layananId,
        Dictionary<string, StockPositionModel> positionsByLocation)
    {
        if (positionsByLocation.TryGetValue(layananId, out var existing))
            return existing;

        var empty = StockPositionModel.CreateEmpty(
            BrgObatType.Key(scope.BrgId),
            ReceiptSourceType.Create(scope.ReceiptSourceId),
            LayananType.Key(layananId));
        positionsByLocation[layananId] = empty;
        return empty;
    }

    private static StockMovementModel RequireMovement(LegacySyncIntentType intent)
        => intent.ProposedMovement
           ?? throw new InvalidOperationException(
               $"Intent kind '{intent.Kind}' requires ProposedMovement.");

    private static IReadOnlyList<StockPositionModel> DeduplicatePositions(
        IReadOnlyList<StockPositionModel> positions)
    {
        var map = new Dictionary<string, StockPositionModel>(StringComparer.Ordinal);
        foreach (var position in positions)
            map[position.LayananId] = position;
        return map.Values.ToArray();
    }
}
