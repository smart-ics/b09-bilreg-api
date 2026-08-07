using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Stable identity connecting a Stock Movement to the business transaction
/// that caused it (BR-STL-003). Distinct from Receipt Source.
/// </summary>
public record SourceTransactionReferenceType : ISourceTransactionReferenceKey
{
    #region CREATION
    public SourceTransactionReferenceType(string sourceTransactionId)
    {
        Guard.Against.NullOrWhiteSpace(sourceTransactionId, nameof(sourceTransactionId));
        SourceTransactionId = sourceTransactionId;
    }

    public static SourceTransactionReferenceType Create(string sourceTransactionId)
        => new(sourceTransactionId);

    public static SourceTransactionReferenceType Default => new("-");

    public static ISourceTransactionReferenceKey Key(string sourceTransactionId)
        => Default with { SourceTransactionId = sourceTransactionId };
    #endregion

    #region PROPERTIES
    public string SourceTransactionId { get; init; }
    #endregion
}

public interface ISourceTransactionReferenceKey
{
    string SourceTransactionId { get; }
}
