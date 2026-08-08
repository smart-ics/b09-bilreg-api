using System.Collections.ObjectModel;
using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Mechanism-neutral Synchronization Position: opaque token + algorithm version.
/// Does not encode a timestamp+id watermark schema (ADR-stock-ledger-legacy-change-discovery).
/// </summary>
public sealed class SynchronizationPositionType : IEquatable<SynchronizationPositionType>
{
    private readonly ReadOnlyCollection<byte> _opaqueValue;

    #region CREATION
    private SynchronizationPositionType(byte[] opaqueValue, string algorithmVersion)
    {
        Guard.Against.Null(opaqueValue, nameof(opaqueValue));
        if (opaqueValue.Length == 0)
            throw new ArgumentException("Opaque Synchronization Position value must not be empty.", nameof(opaqueValue));
        Guard.Against.NullOrWhiteSpace(algorithmVersion, nameof(algorithmVersion));

        _opaqueValue = Array.AsReadOnly((byte[])opaqueValue.Clone());
        AlgorithmVersion = algorithmVersion;
    }

    /// <summary>
    /// Creates a Synchronization Position from an opaque byte sequence and algorithm version.
    /// Bytes are copied; callers may not mutate the stored value through the input array.
    /// </summary>
    public static SynchronizationPositionType Create(byte[] opaqueValue, string algorithmVersion)
        => new(opaqueValue, algorithmVersion);

    /// <summary>
    /// Convenience factory when the opaque token is already a non-empty string (e.g. hex / base64).
    /// </summary>
    public static SynchronizationPositionType CreateFromUtf8Token(string opaqueToken, string algorithmVersion)
    {
        Guard.Against.NullOrWhiteSpace(opaqueToken, nameof(opaqueToken));
        return new(System.Text.Encoding.UTF8.GetBytes(opaqueToken), algorithmVersion);
    }
    #endregion

    #region PROPERTIES
    public IReadOnlyList<byte> OpaqueValue => _opaqueValue;
    public string AlgorithmVersion { get; }
    #endregion

    #region EQUALITY
    public bool Equals(SynchronizationPositionType? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (!string.Equals(AlgorithmVersion, other.AlgorithmVersion, StringComparison.Ordinal))
            return false;
        if (_opaqueValue.Count != other._opaqueValue.Count)
            return false;

        for (var i = 0; i < _opaqueValue.Count; i++)
        {
            if (_opaqueValue[i] != other._opaqueValue[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => Equals(obj as SynchronizationPositionType);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AlgorithmVersion, StringComparer.Ordinal);
        foreach (var b in _opaqueValue)
            hash.Add(b);
        return hash.ToHashCode();
    }

    public static bool operator ==(SynchronizationPositionType? left, SynchronizationPositionType? right)
        => Equals(left, right);

    public static bool operator !=(SynchronizationPositionType? left, SynchronizationPositionType? right)
        => !Equals(left, right);
    #endregion
}
