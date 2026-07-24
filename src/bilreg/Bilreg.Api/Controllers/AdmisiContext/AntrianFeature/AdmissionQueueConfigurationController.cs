using Bilreg.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using System.Security.Claims;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[ApiController]
[Authorize(Policy = AdmissionQueueConfigurationPolicies.PolicyName)]
[Route("api/v1/admission-queue/configuration")]
public sealed class AdmissionQueueConfigurationController : ControllerBase
{
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role)
            .Concat(User.FindAll("role"))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new JSendOk(new
        {
            permission = AdmissionQueueConfigurationPolicies.PermissionId,
            email,
            userName = name,
            roles
        }));
    }
}
