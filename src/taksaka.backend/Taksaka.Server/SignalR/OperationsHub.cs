using Microsoft.AspNetCore.SignalR;
using Taksaka.Core.Enums;

namespace Taksaka.Server.SignalR;

public sealed class OperationsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public Task PublishHealthChanged(string dimension, HealthState state) =>
        Clients.All.SendAsync("HealthChanged", dimension, state.ToString());
}
