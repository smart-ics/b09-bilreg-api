namespace Taksaka.Workers.Maintenance.Models;

internal sealed class AntrianConsistencyItem
{
    public string AntrianId { get; init; } = string.Empty;

    public int NoUrut { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public string ReffId { get; init; } = string.Empty;

    public string ReffDesc { get; init; } = string.Empty;

    public string? BookingUlid { get; init; }

    public string? BookingBridgeId { get; init; }

    public string? FsKdBooking { get; init; }

    public string? RegistrationId { get; init; }

    public DateTime? FdTglMasuk { get; init; }
}
