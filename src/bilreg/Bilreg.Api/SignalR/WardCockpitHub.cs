using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bilreg.Api.SignalR;

[Authorize]
public class WardCockpitHub : Hub
{
    public async Task JoinWard(string bangsalId)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return;

        var groupName = WardCockpitContracts.GetWardGroupName(bangsalId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveWard(string bangsalId)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return;

        var groupName = WardCockpitContracts.GetWardGroupName(bangsalId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
