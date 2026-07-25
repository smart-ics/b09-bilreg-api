using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Api.Configurations;

public class ErrorHandlerMiddleware
{
    private static readonly Meter Meter = new("Bilreg.Api", "1.0");
    private static readonly Counter<long> Errors = Meter.CreateCounter<long>(
        "bilreg.api.errors");
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
                case AdmissionQueueConfigurationException configEx:
                    statusCode = configEx.Code switch
                    {
                        AdmissionQueueConfigurationErrorCodes.WorkstationNotFound => (int)HttpStatusCode.NotFound,
                        AdmissionQueueConfigurationErrorCodes.DisplayNotFound => (int)HttpStatusCode.NotFound,
                        AdmissionQueueConfigurationErrorCodes.WorkstationInactive => (int)HttpStatusCode.Conflict,
                        AdmissionQueueConfigurationErrorCodes.WorkstationLoketConflict => (int)HttpStatusCode.Conflict,
                        AdmissionQueueConfigurationErrorCodes.DisplayInactive => (int)HttpStatusCode.Conflict,
                        AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired => (int)HttpStatusCode.Conflict,
                        AdmissionQueueConfigurationErrorCodes.Concurrency => (int)HttpStatusCode.Conflict,
                        _ => (int)HttpStatusCode.BadRequest
                    };
                    status = configEx.Code;
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
            Errors.Add(
                1,
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("error_code", status));
            var safeMessage = statusCode == (int)HttpStatusCode.InternalServerError
                ? "The request could not be completed."
                : error.Message;
            var resultObj = new JSend(statusCode, status, safeMessage);
            var result = JsonSerializer.Serialize(resultObj);
            await response.WriteAsync(result);
        }
    }
}
