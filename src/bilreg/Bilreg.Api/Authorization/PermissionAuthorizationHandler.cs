using System.Security.Claims;
using Bilreg.Api.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AdmissionQueueApiOptions _options;

    public PermissionAuthorizationHandler(IOptions<AdmissionQueueApiOptions> options)
    {
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.PermissionId))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var allowedRoles = requirement.PermissionId switch
        {
            AdmissionQueueConfigurationPolicies.PermissionId => _options.ConfigurationAllowedRoles,
            AdmissionQueueSupervisorOperationPolicies.PermissionId => _options.SupervisorOperationAllowedRoles,
            _ => null
        };
        if (allowedRoles is null)
        {
            return Task.CompletedTask;
        }

        var configuredRoles = (allowedRoles ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (configuredRoles.Count == 0)
            return Task.CompletedTask;

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("role"))
            .Select(c => c.Value?.Trim() ?? string.Empty)
            .Where(v => v.Length > 0);

        if (roles.Any(role => configuredRoles.Contains(role)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
