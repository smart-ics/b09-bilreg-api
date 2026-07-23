namespace Bilreg.Api.SignalR;

/// <summary>
/// Stable wire contract for admission-queue display refresh hints.
/// Messages are non-authoritative; clients must reload GET displays/current.
/// </summary>
public static class AdmissionQueueRefreshContracts
{
    public const string HubPath = "/hubs/admission-queue";
    public const string RefreshHintEvent = "RefreshHint";
}

public sealed record AdmissionQueueRefreshHint(string? LoketKey);
