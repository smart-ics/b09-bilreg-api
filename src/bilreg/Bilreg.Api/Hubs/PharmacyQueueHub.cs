using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bilreg.Api.Hubs;

/// <summary>
/// Stable wire contracts for Pharmacy Queue SignalR messaging.
/// </summary>
public static class PharmacyQueueContracts
{
    public const string HubPath = "/hubs/pharmacy-queue";
    public const string NotifyWorklistRefreshEvent = "NotifyWorklistRefresh";
    public const string NotifyDisplayRefreshEvent = "NotifyDisplayRefresh";

    public static string DepoGroup(string depoId) => $"depo:{depoId?.Trim() ?? string.Empty}";
    public static string DisplayGroup(string displayId) => $"display:{displayId?.Trim() ?? string.Empty}";
    public static string LoketGroup(string loketKey) => $"loket:{loketKey?.Trim() ?? string.Empty}";
}

/// <summary>
/// Hint-based refresh notification payload for officer worklist.
/// </summary>
public sealed record PharmacyWorklistRefreshHint(string DepoFarmasiId)
{
    public string DepoId => DepoFarmasiId;
}

/// <summary>
/// Hint-based refresh notification payload for display monitors and audio announcement.
/// </summary>
public sealed record PharmacyDisplayRefreshHint(string LoketKey, long AnnouncementVersion);

/// <summary>
/// SignalR Hub for real-time pharmacy queue notifications.
/// Serves queue display screens and pharmacist/officer workstation worklists.
/// Clients receive lightweight hint payloads; data consistency is maintained by fetching snapshots.
/// </summary>
[AllowAnonymous]
public class PharmacyQueueHub : Hub
{
    /// <summary>
    /// Subscribe caller connection to a specific pharmacy depo group.
    /// </summary>
    public async Task SubscribeDepo(string depoId)
    {
        if (!string.IsNullOrWhiteSpace(depoId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PharmacyQueueContracts.DepoGroup(depoId));
        }
    }

    /// <summary>
    /// Unsubscribe caller connection from a specific pharmacy depo group.
    /// </summary>
    public async Task UnsubscribeDepo(string depoId)
    {
        if (!string.IsNullOrWhiteSpace(depoId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PharmacyQueueContracts.DepoGroup(depoId));
        }
    }

    /// <summary>
    /// Subscribe caller connection to a specific display monitor group.
    /// </summary>
    public async Task SubscribeDisplay(string displayId)
    {
        if (!string.IsNullOrWhiteSpace(displayId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PharmacyQueueContracts.DisplayGroup(displayId));
        }
    }

    /// <summary>
    /// Unsubscribe caller connection from a specific display monitor group.
    /// </summary>
    public async Task UnsubscribeDisplay(string displayId)
    {
        if (!string.IsNullOrWhiteSpace(displayId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PharmacyQueueContracts.DisplayGroup(displayId));
        }
    }

    /// <summary>
    /// Convenience alias for SubscribeDepo.
    /// </summary>
    public Task JoinDepoGroup(string depoId) => SubscribeDepo(depoId);

    /// <summary>
    /// Convenience alias for UnsubscribeDepo.
    /// </summary>
    public Task LeaveDepoGroup(string depoId) => UnsubscribeDepo(depoId);

    /// <summary>
    /// Convenience alias for SubscribeDisplay.
    /// </summary>
    public Task JoinDisplayGroup(string displayId) => SubscribeDisplay(displayId);

    /// <summary>
    /// Convenience alias for UnsubscribeDisplay.
    /// </summary>
    public Task LeaveDisplayGroup(string displayId) => UnsubscribeDisplay(displayId);

    /// <summary>
    /// Client-callable broadcast method to notify worklist refresh for a depo.
    /// </summary>
    public async Task NotifyWorklistRefresh(string depoFarmasiId)
    {
        var hint = new PharmacyWorklistRefreshHint(depoFarmasiId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(depoFarmasiId))
        {
            await Clients.All.SendAsync(PharmacyQueueContracts.NotifyWorklistRefreshEvent, hint);
        }
        else
        {
            await Clients.Group(PharmacyQueueContracts.DepoGroup(depoFarmasiId))
                .SendAsync(PharmacyQueueContracts.NotifyWorklistRefreshEvent, hint);
        }
    }

    /// <summary>
    /// Client-callable broadcast method to notify display refresh.
    /// </summary>
    public async Task NotifyDisplayRefresh(string loketKey, long announcementVersion)
    {
        var hint = new PharmacyDisplayRefreshHint(loketKey ?? string.Empty, announcementVersion);
        await Clients.All.SendAsync(PharmacyQueueContracts.NotifyDisplayRefreshEvent, hint);
    }

    /// <summary>
    /// Virtual accessor for HttpContext during handshake, allowing clean unit test verification.
    /// </summary>
    protected virtual HttpContext? GetCallerHttpContext()
    {
        try
        {
            return Context?.GetHttpContext();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Automatically register connection into groups if query parameters are present during handshake.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var httpContext = GetCallerHttpContext();
            if (httpContext != null)
            {
                if (httpContext.Request.Query.TryGetValue("depoId", out var depoId) && !string.IsNullOrWhiteSpace(depoId))
                {
                    await Groups.AddToGroupAsync(Context!.ConnectionId, PharmacyQueueContracts.DepoGroup(depoId!));
                }

                if (httpContext.Request.Query.TryGetValue("displayId", out var displayId) && !string.IsNullOrWhiteSpace(displayId))
                {
                    await Groups.AddToGroupAsync(Context!.ConnectionId, PharmacyQueueContracts.DisplayGroup(displayId!));
                }
            }
        }
        catch
        {
            // Best-effort inspection of connection handshake query
        }

        await base.OnConnectedAsync();
    }
}
