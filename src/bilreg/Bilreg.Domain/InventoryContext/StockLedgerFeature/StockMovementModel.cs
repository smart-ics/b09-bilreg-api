using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Accountable Stock Movement aggregate — completed facts are immutable
/// (BR-STL-022). Corrections and reversals create new movements.
/// </summary>
public record StockMovementModel : IStockMovementKey
{
    private readonly IReadOnlyList<StockMovementLineType> _lines;

    #region CREATION
    private StockMovementModel(
        string stockMovementId,
        SourceTransactionReferenceType sourceTransaction,
        StockMovementKindEnum movementKind,
        DateTime effectiveBusinessTime,
        StockFactOriginEnum origin,
        IReadOnlyList<StockMovementLineType> lines,
        string? reversedMovementId,
        string? correctedMovementId)
    {
        Guard.Against.NullOrWhiteSpace(stockMovementId, nameof(stockMovementId));
        Guard.Against.Null(sourceTransaction, nameof(sourceTransaction));
        Guard.Against.EnumOutOfRange(movementKind, nameof(movementKind));
        Guard.Against.Default(effectiveBusinessTime, nameof(effectiveBusinessTime));
        Guard.Against.EnumOutOfRange(origin, nameof(origin));
        Guard.Against.Null(lines, nameof(lines));
        if (lines.Count == 0)
            throw new ArgumentException("Stock Movement must contain at least one line.", nameof(lines));

        StockMovementId = stockMovementId;
        SourceTransaction = sourceTransaction;
        MovementKind = movementKind;
        EffectiveBusinessTime = effectiveBusinessTime;
        Origin = origin;
        _lines = lines;
        ReversedMovementId = reversedMovementId;
        CorrectedMovementId = correctedMovementId;
    }

    public static StockMovementModel CreateReceipt(
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        IEnumerable<StockMovementLineType> lines,
        StockFactOriginEnum origin,
        string? stockMovementId = null)
        => CreateCompleted(
            StockMovementKindEnum.Receipt,
            sourceTransaction,
            effectiveBusinessTime,
            lines,
            origin,
            stockMovementId,
            reversedMovementId: null,
            correctedMovementId: null,
            requireDirections: StockMovementDirectionEnum.Inbound);

    public static StockMovementModel CreateOutbound(
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        IEnumerable<StockMovementLineType> lines,
        StockFactOriginEnum origin,
        string? stockMovementId = null)
        => CreateCompleted(
            StockMovementKindEnum.Outbound,
            sourceTransaction,
            effectiveBusinessTime,
            lines,
            origin,
            stockMovementId,
            reversedMovementId: null,
            correctedMovementId: null,
            requireDirections: StockMovementDirectionEnum.Outbound);

    public static StockMovementModel CreateTransfer(
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        IEnumerable<StockMovementLineType> lines,
        StockFactOriginEnum origin,
        string? stockMovementId = null)
    {
        var frozen = FreezeLines(lines);
        EnsureTransferInvariants(frozen);
        return CreateCompleted(
            StockMovementKindEnum.Transfer,
            sourceTransaction,
            effectiveBusinessTime,
            frozen,
            origin,
            stockMovementId,
            reversedMovementId: null,
            correctedMovementId: null,
            requireDirections: null);
    }

    public static IStockMovementKey Key(string stockMovementId)
        => new StockMovementKey(stockMovementId);

    private static StockMovementModel CreateCompleted(
        StockMovementKindEnum movementKind,
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        IEnumerable<StockMovementLineType> lines,
        StockFactOriginEnum origin,
        string? stockMovementId,
        string? reversedMovementId,
        string? correctedMovementId,
        StockMovementDirectionEnum? requireDirections)
    {
        Guard.Against.Null(sourceTransaction, nameof(sourceTransaction));
        var frozen = FreezeLines(lines);

        if (requireDirections is { } required)
        {
            if (frozen.Any(x => x.Direction != required))
                throw new ArgumentException(
                    $"{movementKind} movement lines must all be {required}.",
                    nameof(lines));
        }

        return new StockMovementModel(
            string.IsNullOrWhiteSpace(stockMovementId)
                ? Ulid.NewUlid().ToString()
                : stockMovementId,
            SourceTransactionReferenceType.Create(sourceTransaction.SourceTransactionId),
            movementKind,
            effectiveBusinessTime,
            origin,
            frozen,
            reversedMovementId,
            correctedMovementId);
    }

    private static IReadOnlyList<StockMovementLineType> FreezeLines(
        IEnumerable<StockMovementLineType> lines)
    {
        Guard.Against.Null(lines, nameof(lines));
        var list = lines.ToArray();
        if (list.Length == 0)
            throw new ArgumentException("Stock Movement must contain at least one line.", nameof(lines));

        var lineNos = list.Select(x => x.LineNo).ToArray();
        if (lineNos.Distinct().Count() != lineNos.Length)
            throw new ArgumentException("Stock Movement line numbers must be unique.", nameof(lines));

        return Array.AsReadOnly(list);
    }
    #endregion

    #region PROPERTIES
    public string StockMovementId { get; init; }
    public SourceTransactionReferenceType SourceTransaction { get; init; }
    public string SourceTransactionId => SourceTransaction.SourceTransactionId;
    public StockMovementKindEnum MovementKind { get; init; }
    public DateTime EffectiveBusinessTime { get; init; }
    /// <summary>Origin only — never authority.</summary>
    public StockFactOriginEnum Origin { get; init; }
    public IReadOnlyList<StockMovementLineType> Lines => _lines;
    public string? ReversedMovementId { get; init; }
    public string? CorrectedMovementId { get; init; }
    #endregion

    #region BEHAVIOR
    /// <summary>
    /// Creates a new reversing Stock Movement. The original remains unchanged (BR-STL-055/057).
    /// </summary>
    public StockMovementModel Reverse(
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        StockFactOriginEnum origin,
        string? stockMovementId = null)
    {
        Guard.Against.Null(sourceTransaction, nameof(sourceTransaction));

        var reversedLines = _lines
            .OrderBy(x => x.LineNo)
            .Select((line, index) => line.WithOppositeDirection(index + 1))
            .ToArray();

        var reversal = CreateCompleted(
            StockMovementKindEnum.Reversal,
            sourceTransaction,
            effectiveBusinessTime,
            reversedLines,
            origin,
            stockMovementId,
            reversedMovementId: StockMovementId,
            correctedMovementId: null,
            requireDirections: null);

        if (MovementKind == StockMovementKindEnum.Transfer)
            EnsureTransferInvariants(reversal._lines);

        return reversal;
    }

    /// <summary>
    /// Creates a new correcting Stock Movement. The original remains unchanged (BR-STL-055/056).
    /// </summary>
    public StockMovementModel Correct(
        ISourceTransactionReferenceKey sourceTransaction,
        DateTime effectiveBusinessTime,
        IEnumerable<StockMovementLineType> correctionLines,
        StockFactOriginEnum origin,
        string? stockMovementId = null)
    {
        Guard.Against.Null(sourceTransaction, nameof(sourceTransaction));
        var frozen = FreezeLines(correctionLines);

        return CreateCompleted(
            StockMovementKindEnum.Correction,
            sourceTransaction,
            effectiveBusinessTime,
            frozen,
            origin,
            stockMovementId,
            reversedMovementId: null,
            correctedMovementId: StockMovementId,
            requireDirections: null);
    }

    public decimal TotalQuantity(StockMovementDirectionEnum direction)
        => _lines.Where(x => x.Direction == direction).Sum(x => x.Quantity);
    #endregion

    #region INVARIANTS
    private static void EnsureTransferInvariants(IReadOnlyList<StockMovementLineType> lines)
    {
        var outboundLines = lines
            .Where(x => x.Direction == StockMovementDirectionEnum.Outbound)
            .ToArray();
        var inboundLines = lines
            .Where(x => x.Direction == StockMovementDirectionEnum.Inbound)
            .ToArray();

        if (outboundLines.Length == 0 || inboundLines.Length == 0)
            throw new ArgumentException(
                "Transfer must contain both outbound and inbound lines.",
                nameof(lines));

        var outboundQty = outboundLines.Sum(x => x.Quantity);
        var inboundQty = inboundLines.Sum(x => x.Quantity);
        if (outboundQty != inboundQty)
            throw new ArgumentException(
                $"Transfer quantity is not conserved. Outbound={outboundQty}, Inbound={inboundQty}.",
                nameof(lines));

        var sourceLocations = outboundLines.Select(x => x.LayananId).Distinct().ToArray();
        var destinationLocations = inboundLines.Select(x => x.LayananId).Distinct().ToArray();
        if (sourceLocations.Length != 1 || destinationLocations.Length != 1)
            throw new ArgumentException(
                "Transfer must identify exactly one source Stock Location and one destination Stock Location.",
                nameof(lines));

        if (sourceLocations[0] == destinationLocations[0])
            throw new ArgumentException(
                "Transfer source and destination Stock Locations must differ.",
                nameof(lines));

        var provenanceGroups = lines.GroupBy(x => (
            x.BrgId,
            x.ReceiptSourceId,
            x.UnitValuation.AmountPerUnit));

        foreach (var group in provenanceGroups)
        {
            var groupOutbound = group
                .Where(x => x.Direction == StockMovementDirectionEnum.Outbound)
                .Sum(x => x.Quantity);
            var groupInbound = group
                .Where(x => x.Direction == StockMovementDirectionEnum.Inbound)
                .Sum(x => x.Quantity);

            if (groupOutbound != groupInbound)
                throw new ArgumentException(
                    "Transfer must conserve quantity per Item, Receipt Source, and Unit Valuation.",
                    nameof(lines));
        }
    }
    #endregion

    private sealed record StockMovementKey(string StockMovementId) : IStockMovementKey;
}

public interface IStockMovementKey
{
    string StockMovementId { get; }
}
