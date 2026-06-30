using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Text.Json;
using Bilreg.Application.Shared.Helpers;

namespace Bilreg.Api.Configurations;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            int statusCode;
            string status;
            switch (error)
            {
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    status = "Not Found";
                    break;
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.UnprocessableEntity;
                    status = "Validation Error";
                    break;
                case InvalidOperationException stale when stale.Message.Contains("stale", StringComparison.OrdinalIgnoreCase):
                    statusCode = (int)HttpStatusCode.Conflict;
                    status = "Conflict";
                    break;
                case InvalidOperationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    status = "Bad Request";
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    status = "Unauthorized";
                    break;
                case TooManyResultsException:
                    statusCode = (int)HttpStatusCode.UnprocessableEntity;
                    status = "Too Many Results";
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    status = "Internal Server Error";
                    break;
            }

            response.StatusCode = statusCode;
            var resultObj = new JSend(statusCode, status, error.Message);
            var result = JsonSerializer.Serialize(resultObj);
            await response.WriteAsync(result);
        }
    }
}
