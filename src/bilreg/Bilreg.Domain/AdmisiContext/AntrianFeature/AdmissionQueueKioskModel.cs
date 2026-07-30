namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAdmissionQueueKioskKey
{
    string StationId { get; }
}

public sealed record AdmissionKioskServicePointMapping(string ServicePointId, int SortOrder);

public sealed record AdmissionQueueKioskModel : IAdmissionQueueKioskKey
{
    private AdmissionQueueKioskModel(
        string stationId,
        string displayName,
        string locationName,
        bool active,
        int printerProxyPort,
        string notes,
        IReadOnlyList<AdmissionKioskServicePointMapping> servicePoints,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt)
    {
        StationId = NormalizeKey(stationId);
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        LocationName = locationName?.Trim() ?? string.Empty;
        Active = active;
        PrinterProxyPort = NormalizePort(printerProxyPort);
        Notes = notes?.Trim() ?? string.Empty;
        ServicePoints = NormalizeServicePoints(servicePoints);
        RowVersion = rowVersion;
        CreatedBy = createdBy ?? string.Empty;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy ?? string.Empty;
        UpdatedAt = updatedAt;
    }

    public string StationId { get; private init; }
    public string DisplayName { get; private init; }
    public string LocationName { get; private init; }
    public bool Active { get; private init; }
    public int PrinterProxyPort { get; private init; }
    public string Notes { get; private init; }
    public IReadOnlyList<AdmissionKioskServicePointMapping> ServicePoints { get; private init; }
    public long RowVersion { get; private init; }
    public string CreatedBy { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public string UpdatedBy { get; private init; }
    public DateTime UpdatedAt { get; private init; }

    public static AdmissionQueueKioskModel Create(
        string stationId,
        string displayName,
        string locationName,
        bool active,
        int printerProxyPort,
        string notes,
        IEnumerable<AdmissionKioskServicePointMapping> servicePoints,
        string userId,
        DateTime now)
    {
        var model = new AdmissionQueueKioskModel(
            stationId, displayName, locationName, active, printerProxyPort, notes,
            servicePoints.ToList(), 1, userId, now, userId, now);
        model.EnsureActiveMappingValid();
        return model;
    }

    public static AdmissionQueueKioskModel Load(
        string stationId,
        string displayName,
        string locationName,
        bool active,
        int printerProxyPort,
        string notes,
        IEnumerable<AdmissionKioskServicePointMapping> servicePoints,
        long rowVersion,
        string createdBy,
        DateTime createdAt,
        string updatedBy,
        DateTime updatedAt) =>
        new(stationId, displayName, locationName, active, printerProxyPort, notes,
            servicePoints.ToList(), rowVersion, createdBy, createdAt, updatedBy, updatedAt);

    public static IAdmissionQueueKioskKey Key(string stationId) =>
        new AdmissionQueueKioskKey(NormalizeKey(stationId));

    public AdmissionQueueKioskModel Update(
        string displayName,
        string locationName,
        int printerProxyPort,
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
            PrinterProxyPort = NormalizePort(printerProxyPort),
            Notes = notes?.Trim() ?? string.Empty,
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
    }

    public AdmissionQueueKioskModel ReplaceServicePoints(
        IEnumerable<AdmissionKioskServicePointMapping> servicePoints,
        long expectedRowVersion,
        string userId,
        DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        var next = this with
        {
            ServicePoints = NormalizeServicePoints(servicePoints.ToList()),
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
        next.EnsureActiveMappingValid();
        return next;
    }

    public AdmissionQueueKioskModel Activate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        var next = this with
        {
            Active = true,
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
        next.EnsureActiveMappingValid();
        return next;
    }

    public AdmissionQueueKioskModel Deactivate(long expectedRowVersion, string userId, DateTime now)
    {
        EnsureRowVersion(expectedRowVersion);
        return this with
        {
            Active = false,
            RowVersion = RowVersion + 1,
            UpdatedBy = userId,
            UpdatedAt = now
        };
    }

    public void EnsureCanBoot()
    {
        if (!Active)
            throw new InvalidOperationException($"Kiosk '{StationId}' is inactive.");
        if (ServicePoints.Count == 0)
            throw new InvalidOperationException($"Kiosk '{StationId}' has no service point mapping.");
    }

    private void EnsureActiveMappingValid()
    {
        if (Active && ServicePoints.Count == 0)
            throw new InvalidOperationException($"Active kiosk '{StationId}' requires at least one service point.");
    }

    private void EnsureRowVersion(long expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
            throw new InvalidOperationException("stale configuration row version");
    }

    private static IReadOnlyList<AdmissionKioskServicePointMapping> NormalizeServicePoints(
        IReadOnlyList<AdmissionKioskServicePointMapping> servicePoints) =>
        servicePoints
            .Select((x, i) => new AdmissionKioskServicePointMapping(
                NormalizeKey(x.ServicePointId), x.SortOrder >= 0 ? x.SortOrder : i))
            .GroupBy(x => x.ServicePointId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.SortOrder).First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ServicePointId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int NormalizePort(int value)
    {
        if (value is < 1 or > 65535)
            throw new ArgumentException("PrinterProxyPort must be between 1 and 65535.");
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

    private sealed record AdmissionQueueKioskKey(string StationId) : IAdmissionQueueKioskKey;
}
