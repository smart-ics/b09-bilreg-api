using Bilreg.Application.AdmisiRanapContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Filters;

/// <summary>Protects only the new journey read contract during coordinated consumer rollout.</summary>
public sealed class JourneyEndpointsEnabledFilter : IAsyncActionFilter
{
    private readonly AdmisiRanapOptions _options;

    public JourneyEndpointsEnabledFilter(IOptions<AdmisiRanapOptions> options) => _options = options.Value;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_options.JourneyEndpointsEnabled)
            return next();

        context.Result = new ObjectResult(new JSend(
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            "API perjalanan Rawat Inap dinonaktifkan (AdmisiRanap:JourneyEndpointsEnabled = false)."))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
        return Task.CompletedTask;
    }
}
