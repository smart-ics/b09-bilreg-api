using Microsoft.AspNetCore.Authorization;

namespace Bilreg.Api.Authorization;

public sealed class PermissionRequirement(string permissionId) : IAuthorizationRequirement
{
    public string PermissionId { get; } = permissionId;
}
