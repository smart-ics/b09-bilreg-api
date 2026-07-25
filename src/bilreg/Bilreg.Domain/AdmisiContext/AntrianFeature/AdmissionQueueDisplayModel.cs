namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAdmissionQueueDisplayKey
{
    string DisplayId { get; }
}

public sealed record AdmissionDisplayLoketMapping(string LoketKey, int SortOrder);

public sealed record AdmissionQueueDisplayModel : IAdmissionQueueDisplayKey
{
    private AdmissionQueueDisplayModel(
        string displayId,
        string displayName,
        string locationName,
        bool active,
        bool audioEnabled,
        int pollIntervalMs,
        string layoutKey,
        string notes,
        IReadOnlyList<AdmissionDisplayLoketMapping> lokets,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt)
    {
        DisplayId = NormalizeKey(displayId);
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        LocationName = locationName?.Trim() ?? string.Empty;
        Active = active;
        AudioEnabled = audioEnabled;
        PollIntervalMs = NormalizePoll(pollIntervalMs);
        LayoutKey = layoutKey?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        Lokets = NormalizeLokets(lokets);
        RowVersion = rowVersion;
        CreatedBy = createdBy ?? string.Empty;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy ?? string.Empty;
        UpdatedAt = updatedAt;
    }

    public string DisplayId { get; private init; }
    public string DisplayName { get; private init; }
    public string LocationName { get; private init; }
    public bool Active { get; private init; }
    public bool AudioEnabled { get; private init; }
    public int PollIntervalMs { get; private init; }
    public string LayoutKey { get; private init; }
    public string Notes { get; private init; }
    public IReadOnlyList<AdmissionDisplayLoketMapping> Lokets { get; private init; }
    public long RowVersion { get; private init; }
    public string CreatedBy { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public string UpdatedBy { get; private init; }
    public DateTime UpdatedAt { get; private init; }

    public static AdmissionQueueDisplayModel Create(
        string displayId,
        string displayName,
        string locationName,
        bool active,
        bool audioEnabled,
        int pollIntervalMs,
        string layoutKey,
        string notes,
        IEnumerable<AdmissionDisplayLoketMapping> lokets,
        string userId,
        DateTime now)
    {
        var model = new AdmissionQueueDisplayModel(
            displayId, displayName, locationName, active, audioEnabled, pollIntervalMs,
            layoutKey, notes, lokets.ToList(), 1, userId, now, userId, now);
        model.EnsureActiveMappingValid();
        return model;
    }

    public static AdmissionQueueDisplayModel Load(
        string displayId,
        string displayName,
        string locationName,
        bool active,
        bool audioEnabled,
        int pollIntervalMs,
        string layoutKey,
        string notes,
        IEnumerable<AdmissionDisplayLoketMapping> lokets,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt) =>
        new(displayId, displayName, locationName, active, audioEnabled, pollIntervalMs,
            layoutKey, notes, lokets.ToList(), rowVersion, createdBy, createdAt, updatedBy, updatedAt);

    public static IAdmissionQueueDisplayKey Key(string displayId) =>
        new AdmissionQueueDisplayKey(NormalizeKey(displayId));

    public AdmissionQueueDisplayModel Update(
        string displayName,
        string locationName,
        bool audioEnabled,
        int pollIntervalMs,
        string layoutKey,
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
            AudioEnabled = audioEnabled,
            PollIntervalMs = NormalizePoll(pollIntervalMs),
            LayoutKey = layoutKey?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
    }

    public AdmissionQueueDisplayModel ReplaceLokets(
        IEnumerable<AdmissionDisplayLoketMapping> lokets,
        long expectedRowVersion,
        string userId,
        DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        var next = this with
        {
            Lokets = NormalizeLokets(lokets.ToList()),
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
        next.EnsureActiveMappingValid();
        return next;
    }

    public AdmissionQueueDisplayModel Activate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        var next = this with { Active = true, RowVersion = RowVersion + 1, UpdatedBy = userId, UpdatedAt = now };
        next.EnsureActiveMappingValid();
        return next;
    }

    public AdmissionQueueDisplayModel Deactivate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        return this with { Active = false, RowVersion = RowVersion + 1, UpdatedBy = userId, UpdatedAt = now };
    }

    public void EnsureCanBoot()
    {
        if (!Active)
            throw new InvalidOperationException($"Display '{DisplayId}' is inactive.");
        if (Lokets.Count == 0)
            throw new InvalidOperationException($"Display '{DisplayId}' has no loket mapping.");
    }

    public void EnsureActiveMappingValid()
    {
        if (Active && Lokets.Count == 0)
            throw new InvalidOperationException($"Active display '{DisplayId}' requires at least one loket.");
    }

    private void EnsureRowVersion(long expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
            throw new InvalidOperationException("stale configuration row version");
    }

    private static IReadOnlyList<AdmissionDisplayLoketMapping> NormalizeLokets(
        IReadOnlyList<AdmissionDisplayLoketMapping> lokets)
    {
        var normalized = lokets
            .Select((x, i) => new AdmissionDisplayLoketMapping(NormalizeKey(x.LoketKey), x.SortOrder >= 0 ? x.SortOrder : i))
            .GroupBy(x => x.LoketKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.SortOrder).First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.LoketKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return normalized;
    }

    private static int NormalizePoll(int value)
    {
        if (value is < 1000 or > 120000)
            throw new ArgumentException("PollIntervalMs must be between 1000 and 120000.");
        return value;
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

    private sealed record AdmissionQueueDisplayKey(string DisplayId) : IAdmissionQueueDisplayKey;
}
