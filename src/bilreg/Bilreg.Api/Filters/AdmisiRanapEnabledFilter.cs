using Bilreg.Application.AdmisiRanapContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Filters;

public class AdmisiRanapEnabledFilter : IAsyncActionFilter
{
    private readonly AdmisiRanapOptions _options;

    public AdmisiRanapEnabledFilter(IOptions<AdmisiRanapOptions> options) =>
        _options = options.Value;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_options.Enabled)
            return next();

        context.Result = new ObjectResult(new JSend(
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            "Modul Admisi Rawat Inap dinonaktifkan (AdmisiRanap:Enabled = false)."))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };

        return Task.CompletedTask;
    }
}
