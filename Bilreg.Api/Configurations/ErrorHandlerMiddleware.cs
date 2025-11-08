using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nuna.Lib.ActionResultHelper;
using System.Net;
using System.Text.Json;

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

            string? status;
            switch (error)
            {
                case ApplicationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    status = "Too Much Data";
                    break;
                case ArgumentException:
                case ValidationException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    status = "Bad Request";
                    break;
                case KeyNotFoundException:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    status = "Data Not Found";
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