namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public enum AdmissionServicePointStatusEnum
{
    Retired = 0,
    Active = 1
}

public interface IAdmissionServicePointKey
{
    string ServicePointId { get; }
}

public sealed record AdmissionServicePointModel : IAdmissionServicePointKey
{
    private AdmissionServicePointModel(string servicePointId, string displayName,
        string queuePrefix, AdmissionServicePointStatusEnum status)
    {
        ServicePointId = NormalizeId(servicePointId);
        DisplayName = NormalizeName(displayName);
        QueuePrefix = NormalizePrefix(queuePrefix);
        Status = status;
    }

    public string ServicePointId { get; private init; }
    public string DisplayName { get; private init; }
    public string QueuePrefix { get; private init; }
    public AdmissionServicePointStatusEnum Status { get; private init; }
    public bool IsActive => Status == AdmissionServicePointStatusEnum.Active;

    public static AdmissionServicePointModel Create(string id, string name, string prefix) =>
        new(id, name, prefix, AdmissionServicePointStatusEnum.Active);

    public static AdmissionServicePointModel Load(string id, string name, string prefix,
        AdmissionServicePointStatusEnum status) => new(id, name, prefix, status);

    public static IAdmissionServicePointKey Key(string id) =>
        new AdmissionServicePointKey(NormalizeId(id));

    public AdmissionServicePointModel Retire() => this with { Status = AdmissionServicePointStatusEnum.Retired };
    public AdmissionServicePointModel Rename(string name) => this with { DisplayName = NormalizeName(name) };
    public AdmissionServicePointModel ChangePrefix(string prefix) => this with { QueuePrefix = NormalizePrefix(prefix) };

    public void EnsureCanAcceptIntake()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Service Point '{ServicePointId}' is retired.");
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ServicePointId is required.", nameof(value));
        return value.Trim();
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Display name is required.", nameof(value));
        return value.Trim();
    }

    public static string NormalizePrefix(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 1 || normalized[0] is < 'A' or > 'Z')
            throw new ArgumentException("Queue Prefix must be one uppercase ASCII letter (A-Z).", nameof(value));
        return normalized;
    }

    private sealed record AdmissionServicePointKey(string ServicePointId) : IAdmissionServicePointKey;
}
