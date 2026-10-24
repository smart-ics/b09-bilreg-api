namespace Bilreg.Api.SignalR;

public static class WardCockpitContracts
{
    public const string HubPath = "/hubs/ward-cockpit";
    public const string GroupPrefix = "Ward-";

    public const string OnBedStatusChangedEvent = "OnBedStatusChanged";
    public const string OnWaitingListUpdatedEvent = "OnWaitingListUpdated";
    public const string OnKpiUpdatedEvent = "OnKpiUpdated";

    public static string GetWardGroupName(string bangsalId) => $"{GroupPrefix}{bangsalId}";
}
