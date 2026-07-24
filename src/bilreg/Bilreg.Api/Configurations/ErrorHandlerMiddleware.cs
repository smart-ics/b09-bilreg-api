using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Text.Json;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;

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
                case AdmissionQueueConcurrencyException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    status = "AQ_CONCURRENCY_CONFLICT";
                    break;
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    status = "AQ_RESOURCE_NOT_FOUND";
                    break;
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    status = "AQ_INVALID_REQUEST";
                    break;
                case SequenceExhaustedException:
                    statusCode = (int)HttpStatusCode.ServiceUnavailable;
                    status = "AQ_SEQUENCE_EXHAUSTED";
                    break;
                case InvalidOperationException stale when stale.Message.Contains("stale", StringComparison.OrdinalIgnoreCase):
                    statusCode = (int)HttpStatusCode.Conflict;
                    status = "AQ_CONCURRENCY_CONFLICT";
                    break;
                case InvalidOperationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    status = "AQ_OPERATION_NOT_ALLOWED";
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    status = "AQ_UNAUTHENTICATED";
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
