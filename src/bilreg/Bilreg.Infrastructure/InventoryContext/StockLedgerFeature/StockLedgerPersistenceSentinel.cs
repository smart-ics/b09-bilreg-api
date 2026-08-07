using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

internal static class StockLedgerPersistenceSentinel
{
    public static readonly DateTime EmptyDate = new(3000, 1, 1);
    public static readonly byte[] EmptyOpaque = [];

    public static string NullToEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value;

    public static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    public static DateTime ExpirationToStorage(DateOnly? expirationDate)
        => expirationDate?.ToDateTime(TimeOnly.MinValue) ?? EmptyDate;

    public static DateOnly? ExpirationFromStorage(DateTime expirationDate)
        => expirationDate == EmptyDate
            ? null
            : DateOnly.FromDateTime(expirationDate);

    public static byte[] OpaqueToStorage(SynchronizationPositionType? position)
        => position is null
            ? EmptyOpaque
            : position.OpaqueValue.ToArray();

    public static string AlgorithmVersionToStorage(SynchronizationPositionType? position)
        => position?.AlgorithmVersion ?? string.Empty;

    public static SynchronizationPositionType? PositionFromStorage(
        byte[]? opaque,
        string? algorithmVersion)
    {
        if (opaque is null || opaque.Length == 0)
            return null;
        if (string.IsNullOrWhiteSpace(algorithmVersion))
            return null;
        return SynchronizationPositionType.Create(opaque, algorithmVersion);
    }
}
