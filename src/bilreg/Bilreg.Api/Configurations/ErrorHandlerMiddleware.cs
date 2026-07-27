using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Diagnostics;
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
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
            var succeeded = context.Response.StatusCode < StatusCodes.Status400BadRequest;
            LogAdmissionQueueOperation(
                context,
                succeeded ? "Succeeded" : "Rejected",
                succeeded ? null : $"HTTP_{context.Response.StatusCode}",
                stopwatch.ElapsedMilliseconds);
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
            LogAdmissionQueueOperation(context, "Failed", status, stopwatch.ElapsedMilliseconds);
            var safeMessage = statusCode == (int)HttpStatusCode.InternalServerError
                ? "The request could not be completed."
                : error.Message;
            var resultObj = new JSend(statusCode, status, safeMessage);
            var result = JsonSerializer.Serialize(resultObj);
            await response.WriteAsync(result);
        }
    }

    private void LogAdmissionQueueOperation(HttpContext context, string result, string? failureCategory, long durationMs)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/v1/admission-queue", StringComparison.Ordinal)
            && !path.EndsWith("/direct", StringComparison.Ordinal))
            return;

        _logger.LogInformation(
            "AdmissionQueueOperationalEvent {Operation} {Result} {FailureCategory} {DurationMs} {BusinessDate} {ServicePointId} {WorkstationKey} {LoketKey}",
            GetOperationName(path),
            result,
            failureCategory ?? "None",
            durationMs,
            context.Request.Query["businessDate"].ToString() is { Length: > 0 } businessDate ? businessDate : "Unspecified",
            context.Request.Query["servicePointId"].ToString() is { Length: > 0 } servicePointId ? servicePointId : "Unspecified",
            context.Request.Headers["X-Workstation-Key"].ToString() is { Length: > 0 } workstationKey ? workstationKey : "Unspecified",
            context.Request.Headers["X-Loket-Key"].ToString() is { Length: > 0 } loketKey ? loketKey : "Unspecified");
    }

    public static string GetOperationName(string path) => path switch
    {
        "/api/v1/admission-queue/close" => "QueueClose",
        "/api/v1/admission-queue/closing-preview" => "QueueClosingPreview",
        "/api/reg/rajalWalkIn/direct" => "DirectWalkInRegistration",
        "/api/reg/rajalByBooking/direct" => "DirectBookingRegistration",
        _ when path.EndsWith("/call", StringComparison.Ordinal) => "Call",
        _ when path.EndsWith("/recall", StringComparison.Ordinal) => "Recall",
        _ when path.EndsWith("/return-to-waiting", StringComparison.Ordinal) => "ReturnToWaiting",
        _ when path.EndsWith("/start-service", StringComparison.Ordinal) => "StartService",
        _ when path.EndsWith("/cancel-registration", StringComparison.Ordinal) => "CancelRegistration",
        _ when path.EndsWith("/outcomes/established", StringComparison.Ordinal) => "Established",
        _ => "AdmissionQueueOperation"
    };
}
