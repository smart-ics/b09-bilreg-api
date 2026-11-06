namespace Bilreg.Api.SignalR;

/// <summary>
/// Stable wire contract for clinic-queue display refresh hints.
/// Messages are non-authoritative; clients must reload GET /api/v1/clinic-queue/displays/current.
/// </summary>
public static class ClinicQueueRefreshContracts
{
    public const string HubPath = "/hubs/admission-queue";
    public const string RefreshHintEvent = "ClinicQueueRefreshHint";
}
