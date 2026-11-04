using System.Net;
using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Application.LabContext.LabOrderFeature;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class DuplicateLabOrderErrorHandlerTest
{
    private readonly ILogger<ErrorHandlerMiddleware> _logger = NullLogger<ErrorHandlerMiddleware>.Instance;

    [Fact]
    public async Task Invoke_WhenDuplicateLabOrderWarningExceptionThrown_Returns409WithStructuredJSendPayload()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        const string expectedMessage = "EX-05: Peringatan duplikasi tes laboratorium dalam 24 jam terakhir.";
        var expectedDuplicateTestNames = new[] { "Darah Lengkap (CBC 5 Diff)", "Glukosa Puasa" };
        var expectedPreviousOrderIds = new[] { "ORD-LAB-2610-0012", "ORD-LAB-2610-0015" };

        RequestDelegate next = _ => throw new DuplicateLabOrderWarningException(
            expectedMessage,
            expectedDuplicateTestNames,
            expectedPreviousOrderIds);

        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("DUPLICATE_LAB_ORDER_WARNING");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);

        var dataElement = root.GetProperty("data");
        dataElement.ValueKind.Should().Be(JsonValueKind.Object);

        var duplicateTestNames = dataElement.GetProperty("duplicateTestNames")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        duplicateTestNames.Should().BeEquivalentTo(expectedDuplicateTestNames);

        var previousOrderIds = dataElement.GetProperty("previousOrderIds")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        previousOrderIds.Should().BeEquivalentTo(expectedPreviousOrderIds);
    }

    [Fact]
    public async Task Invoke_WhenDuplicateLabOrderWarningExceptionThrown_WithEmptyCollections_ReturnsEmptyArrays()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new DuplicateLabOrderWarningException(
            "Peringatan tes duplikat.");

        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("DUPLICATE_LAB_ORDER_WARNING");
        root.GetProperty("message").GetString().Should().Be("Peringatan tes duplikat.");

        var duplicateTestNames = root.GetProperty("data").GetProperty("duplicateTestNames")
            .EnumerateArray()
            .ToList();
        duplicateTestNames.Should().BeEmpty();

        var previousOrderIds = root.GetProperty("data").GetProperty("previousOrderIds")
            .EnumerateArray()
            .ToList();
        previousOrderIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_WhenNonDuplicateExceptionThrown_PreservesStandardBehavior()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new ArgumentException("Parameter tidak valid.");

        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        // Standard JSend implementation in Nuna.Lib ActionResultHelper maps:
        // status -> "AQ_INVALID_REQUEST"
        // code -> "400"
        // data -> error.Message
        root.GetProperty("status").GetString().Should().Be("AQ_INVALID_REQUEST");
        root.GetProperty("code").GetString().Should().Be("400");
        root.GetProperty("data").GetString().Should().Be("Parameter tidak valid.");
    }

    [Fact]
    public async Task Invoke_WhenSuccessfulRequest_DoesNotModifyResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
