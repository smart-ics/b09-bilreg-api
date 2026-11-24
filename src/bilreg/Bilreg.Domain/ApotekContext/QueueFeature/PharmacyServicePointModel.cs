using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.QueueFeature;

public class PharmacyServicePointModel : IPharmacyServicePointKey
{
    private PharmacyServicePointModel(
        string servicePointId,
        string depoFarmasiId,
        string displayName,
        string queuePrefix,
        bool active)
    {
        ServicePointId = servicePointId;
        DepoFarmasiId = depoFarmasiId;
        DisplayName = displayName;
        QueuePrefix = queuePrefix;
        Active = active;
    }

    public string ServicePointId { get; private set; }
    public string DepoFarmasiId { get; private set; }
    public string DisplayName { get; private set; }
    public string QueuePrefix { get; private set; }
    public bool Active { get; private set; }

    public static IPharmacyServicePointKey Key(string servicePointId)
        => new ServicePointKey(NormalizeRequired(servicePointId, nameof(servicePointId)));

    public static PharmacyServicePointModel Create(
        string servicePointId,
        string depoFarmasiId,
        string displayName,
        string queuePrefix)
    {
        var id = NormalizeRequired(servicePointId, nameof(servicePointId));
        var depo = NormalizeRequired(depoFarmasiId, nameof(depoFarmasiId));
        var name = NormalizeRequired(displayName, nameof(displayName));
        var prefix = NormalizePrefix(queuePrefix);

        return new PharmacyServicePointModel(id, depo, name, prefix, active: true);
    }

    public static PharmacyServicePointModel Rehydrate(
        string servicePointId,
        string depoFarmasiId,
        string displayName,
        string queuePrefix,
        bool active)
    {
        var id = NormalizeRequired(servicePointId, nameof(servicePointId));
        var depo = NormalizeRequired(depoFarmasiId, nameof(depoFarmasiId));
        var name = NormalizeRequired(displayName, nameof(displayName));
        var prefix = NormalizePrefix(queuePrefix);

        return new PharmacyServicePointModel(id, depo, name, prefix, active);
    }

    public void Deactivate()
    {
        Active = false;
    }

    public void Activate()
    {
        Active = true;
    }

    public void Update(string displayName, string queuePrefix)
    {
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        QueuePrefix = NormalizePrefix(queuePrefix);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
        return value.Trim();
    }

    private static string NormalizePrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Queue prefix is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length is < 1 or > 5)
            throw new ArgumentException("Queue prefix must be between 1 and 5 characters.", nameof(value));
        return normalized;
    }

    private sealed record ServicePointKey(string ServicePointId) : IPharmacyServicePointKey;
}
