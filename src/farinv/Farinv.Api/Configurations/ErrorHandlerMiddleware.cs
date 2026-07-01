using System.Net;
using System.Text.Json;
using Bilreg.Application.Helpers;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Configurations;

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

            string? status;
            switch (error)
            {
                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    status = "Bad Request";
                    break;
                case KeyNotFoundException:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    status = "Data Not Found";
                    break;
                case TooManyResultsException:
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    status = "Too Many Results";
                    break;
                default:
                    // unhandled error
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    status = "Internal Server Error";
                    break;
            }

            var resultObj = new JSend(response.StatusCode, status, error.Message);
            var result = JsonSerializer.Serialize(resultObj);
            await response.WriteAsync(result);
        }
    }
}