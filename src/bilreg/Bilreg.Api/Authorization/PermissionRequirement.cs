using Microsoft.AspNetCore.Authorization;

namespace Bilreg.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionId)
    {
        PermissionId = permissionId;
    }

    public string PermissionId { get; }
}
