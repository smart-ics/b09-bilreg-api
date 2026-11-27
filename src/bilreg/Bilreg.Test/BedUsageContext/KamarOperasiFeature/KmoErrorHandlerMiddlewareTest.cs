using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class KmoErrorHandlerMiddlewareTest
{
    private readonly ILogger<ErrorHandlerMiddleware> _logger = NullLogger<ErrorHandlerMiddleware>.Instance;

    private static DefaultHttpContext CreateHttpContext(string path, ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (user != null)
        {
            context.User = user;
        }
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        return JsonDocument.Parse(responseBody);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndPreOpChecklistIncompleteException_Returns422With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/PreOpClearance/Save";
        const string expectedMessage = "Pre-Operative Clearance tidak dapat disetujui karena checklist belum lengkap.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new PreOpChecklistIncompleteException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("KMO_PREOP_CHECKLIST_INCOMPLETE");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndScheduleWarningException_Returns422WithStructuredCamelCaseData()
    {
        // Arrange
        const string path = "/api/ScheduleOp/SaveWorkbench";
        const string expectedMessage = "Jadwal operasi memiliki potensi konflik peringatan.";
        var expectedWarnings = new List<string>
        {
            "Jadwal berdekatan dengan operasi sebelumnya (< 30 menit).",
            "Dokter operator memiliki jadwal lain pada waktu bersamaan."
        };
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new ScheduleWarningException(expectedMessage, expectedWarnings);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("KMO_SCHEDULE_WARNING");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);

        var dataElement = root.GetProperty("data");
        dataElement.ValueKind.Should().Be(JsonValueKind.Object);

        // Verify camelCase naming policy (not PascalCase)
        dataElement.TryGetProperty("Warnings", out _).Should().BeFalse();
        dataElement.TryGetProperty("RequireOverride", out _).Should().BeFalse();

        var warnings = dataElement.GetProperty("warnings")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        warnings.Should().BeEquivalentTo(expectedWarnings);

        dataElement.GetProperty("requireOverride").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndScheduleCollisionException_Returns409With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/ScheduleOp/SetSchedule";
        const string expectedMessage = "Kamar Operasi 1 sudah terpakai pada rentang waktu yang dipilih.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new ScheduleCollisionException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("KMO_SCHEDULE_COLLISION");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndInvalidOperationException_Returns400With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/Operation/Start";
        const string expectedMessage = "Operasi tidak dapat dimulai karena status belum valid.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new InvalidOperationException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("AQ_OPERATION_NOT_ALLOWED");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndArgumentException_Returns400With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/OrderOp/CreateByReg";
        const string expectedMessage = "Parameter registrasi operasi tidak valid.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new ArgumentException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("AQ_INVALID_REQUEST");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndKeyNotFoundException_Returns404With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/ScheduleOp/SCH-999";
        const string expectedMessage = "Jadwal operasi dengan ID SCH-999 tidak ditemukan.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new KeyNotFoundException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("AQ_RESOURCE_NOT_FOUND");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndUnauthorizedAccessException_WhenAuthenticated_Returns403With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/PreOpClearance/Save";
        const string expectedMessage = "Akses ditolak untuk peran pengguna saat ini.";
        var claims = new[] { new Claim(ClaimTypes.Name, "doctor1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateHttpContext(path, principal);

        RequestDelegate next = _ => throw new UnauthorizedAccessException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("AQ_FORBIDDEN");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndUnauthorizedAccessException_WhenUnauthenticated_Returns401With4FieldEnvelope()
    {
        // Arrange
        const string path = "/api/PreOpClearance/Save";
        const string expectedMessage = "Pengguna belum terotentikasi.";
        var context = CreateHttpContext(path); // unauthenticated

        RequestDelegate next = _ => throw new UnauthorizedAccessException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("AQ_UNAUTHENTICATED");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenKmoRoute_AndUnhandledException_Returns500WithErrorStatus()
    {
        // Arrange
        const string path = "/api/ScheduleOp/SaveWorkbench";
        const string expectedMessage = "Koneksi basis data terputus mendadak.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new Exception(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("code").GetString().Should().Be("AQ_UNHANDLED_EXCEPTION");
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invoke_WhenNonKmoRoute_MaintainsLegacy3FieldFormat()
    {
        // Arrange
        const string path = "/api/Ward/SomeAction";
        const string expectedMessage = "Parameter ruangan rawat inap tidak valid.";
        var context = CreateHttpContext(path);

        RequestDelegate next = _ => throw new ArgumentException(expectedMessage);
        var middleware = new ErrorHandlerMiddleware(next, _logger);

        // Act
        await middleware.Invoke(context);

        // Assert HTTP status & headers
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        // Assert JSON body (legacy JSend format)
        using var jsonDoc = await ReadResponseBodyAsync(context);
        var root = jsonDoc.RootElement;

        // Legacy format: { "status": "AQ_INVALID_REQUEST", "code": "400", "data": "..." }
        root.GetProperty("status").GetString().Should().Be("AQ_INVALID_REQUEST");
        root.GetProperty("code").GetString().Should().Be("400");
        root.GetProperty("data").GetString().Should().Be(expectedMessage);
        root.TryGetProperty("message", out _).Should().BeFalse();
    }
}

