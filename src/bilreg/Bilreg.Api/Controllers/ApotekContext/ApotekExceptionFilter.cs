using Bilreg.Application.Shared;
using Bilreg.Domain.ApotekContext.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bilreg.Api.Controllers.ApotekContext;

public class ApotekExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApotekConcurrencyException ex:
                context.Result = new ObjectResult(new { status = "fail", message = ex.Message, code = "CONCURRENCY" })
                { StatusCode = StatusCodes.Status409Conflict };
                context.ExceptionHandled = true;
                break;
            case ApotekDomainException ex:
                context.Result = new ObjectResult(new { status = "fail", message = ex.Message, code = "DOMAIN" })
                { StatusCode = StatusCodes.Status400BadRequest };
                context.ExceptionHandled = true;
                break;
            case UnauthorizedAccessException ex:
                context.Result = new ObjectResult(new { status = "fail", message = ex.Message, code = "AUTH" })
                { StatusCode = StatusCodes.Status401Unauthorized };
                context.ExceptionHandled = true;
                break;
        }
    }
}

internal static class AptActor
{
    public static string Require(ICurrentUserContext user) => user.GetActorUserId();
}
