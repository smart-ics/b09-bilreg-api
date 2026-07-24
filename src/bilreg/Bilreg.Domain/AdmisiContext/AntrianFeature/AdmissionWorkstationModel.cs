namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAdmissionWorkstationKey
{
    string WorkstationKey { get; }
}

public sealed record AdmissionWorkstationModel : IAdmissionWorkstationKey
{
    private AdmissionWorkstationModel(
        string workstationKey,
        string displayName,
        string locationName,
        string loketKey,
        bool active,
        string notes,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt)
    {
        WorkstationKey = NormalizeKey(workstationKey);
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        LocationName = locationName?.Trim() ?? string.Empty;
        LoketKey = NormalizeKey(loketKey);
        Active = active;
        Notes = notes?.Trim() ?? string.Empty;
        RowVersion = rowVersion;
        CreatedBy = createdBy ?? string.Empty;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy ?? string.Empty;
        UpdatedAt = updatedAt;
    }

    public string WorkstationKey { get; private init; }
    public string DisplayName { get; private init; }
    public string LocationName { get; private init; }
    public string LoketKey { get; private init; }
    public bool Active { get; private init; }
    public string Notes { get; private init; }
    public long RowVersion { get; private init; }
    public string CreatedBy { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public string UpdatedBy { get; private init; }
    public DateTime UpdatedAt { get; private init; }

    public static AdmissionWorkstationModel Create(
        string workstationKey,
        string displayName,
        string locationName,
        string loketKey,
        bool active,
        string notes,
        string userId,
        DateTime now) =>
        new(workstationKey, displayName, locationName, loketKey, active, notes, 1,
            userId, now, userId, now);

    public static AdmissionWorkstationModel Load(
        string workstationKey,
        string displayName,
        string locationName,
        string loketKey,
        bool active,
        string notes,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt) =>
        new(workstationKey, displayName, locationName, loketKey, active, notes, rowVersion,
            createdBy, createdAt, updatedBy, updatedAt);

    public static IAdmissionWorkstationKey Key(string workstationKey) =>
        new AdmissionWorkstationKey(NormalizeKey(workstationKey));

    public AdmissionWorkstationModel Update(
        string displayName,
        string locationName,
        string loketKey,
        string notes,
        long expectedRowVersion,
        string userId,
        DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        return this with
        {
            DisplayName = NormalizeRequired(displayName, nameof(displayName)),
            LocationName = locationName?.Trim() ?? string.Empty,
            LoketKey = NormalizeKey(loketKey),
            Notes = notes?.Trim() ?? string.Empty,
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
    }

    public AdmissionWorkstationModel Activate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        return this with { Active = true, RowVersion = RowVersion + 1, UpdatedBy = userId, UpdatedAt = now };
    }

    public AdmissionWorkstationModel Deactivate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        return this with { Active = false, RowVersion = RowVersion + 1, UpdatedBy = userId, UpdatedAt = now };
    }

    public void EnsureActive()
    {
        if (!Active)
            throw new InvalidOperationException($"Workstation '{WorkstationKey}' is inactive.");
    }

    private void EnsureRowVersion(long expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
            throw new InvalidOperationException("stale configuration row version");
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Key is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > 50)
            throw new ArgumentException("Key may not exceed 50 characters.");
        return trimmed;
    }

    private static string NormalizeRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.");
        return value.Trim();
    }

    private sealed record AdmissionWorkstationKey(string WorkstationKey) : IAdmissionWorkstationKey;
}
