using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.Filters;

/// <summary>
/// Gates Stock Ledger dev-smoke HTTP surface: Development environment +
/// <c>StockLedger:AllowDevSmoke</c>. Disabled → 404 (do not advertise the route).
/// </summary>
public sealed class StockLedgerDevSmokeEnabledFilter : IAsyncActionFilter
{
    private readonly IHostEnvironment _env;
    private readonly StockLedgerCoexistenceOptions _options;

    public StockLedgerDevSmokeEnabledFilter(
        IHostEnvironment env,
        IOptions<StockLedgerCoexistenceOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_env.IsDevelopment() && _options.AllowDevSmoke)
            return next();

        context.Result = new NotFoundResult();
        return Task.CompletedTask;
    }
}
