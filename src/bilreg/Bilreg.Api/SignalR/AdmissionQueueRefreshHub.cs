using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bilreg.Api.SignalR;

/// <summary>
/// Passive Queue Display subscription endpoint. Clients receive best-effort
/// <see cref="AdmissionQueueRefreshContracts.RefreshHintEvent"/> messages only;
/// persisted current-Loket snapshots remain recovery truth.
/// </summary>
[Authorize]
public sealed class AdmissionQueueRefreshHub : Hub;
