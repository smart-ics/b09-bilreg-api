using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class AddAntrianEmrByRegServiceTest : IDisposable
{
    private readonly WireMockServer _server;
    private readonly AddAntrianEmrByRegService _sut;

    private const string ENDPOINT_PATH = "/api/Dashboard/addReg";

    private static readonly string SuccessResponseBody = JsonSerializer.Serialize(new
    {
        status = "Succsess",
        code = "200",
        data = new { antrianId = "", noAntrian = 1 }
    });

    private static AddAntrianEmrByRegCommand FakeCommand() => new(
        RegId: "REG-001",
        BookingId: "BOOK-001",
        PasienId: "PAS-001",
        PasienName: "John Doe",
        LayananId: "LAY-001",
        DokterId: "DOK-001",
        TglBerobat: "2026-04-23",
        JamJadwal: "09:00",
        NoAntrian: 5
    );

    public AddAntrianEmrByRegServiceTest()
    {
        // Start WireMock on a random available port
        _server = WireMockServer.Start();

        var opt = Options.Create(new EmrOptions
        {
            BaseApiUrl = _server.Urls[0]
        });

        _sut = new AddAntrianEmrByRegService(opt);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private void StubAddRegEndpoint()
    {
        _server
            .Given(Request.Create()
                .WithPath(ENDPOINT_PATH)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SuccessResponseBody));
    }

    // ─── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public void Execute_ShouldCallCorrectEndpoint()
    {
        // Arrange
        StubAddRegEndpoint();
        var cmd = FakeCommand();

        // Act
        _sut.Execute(cmd);

        // Assert
        var logEntries = _server.FindLogEntries(
            Request.Create().WithPath(ENDPOINT_PATH).UsingAnyMethod()).ToList();

        logEntries.Should().HaveCount(1,
            because: "exactly one request should have been sent to /api/Dashboard/addReg");
        logEntries[0].RequestMessage.Path.Should().Be(ENDPOINT_PATH);
    }

    [Fact]
    public void Execute_ShouldUsePostMethod()
    {
        // Arrange
        StubAddRegEndpoint();
        var cmd = FakeCommand();

        // Act
        _sut.Execute(cmd);

        // Assert
        var logEntries = _server.FindLogEntries(
            Request.Create().WithPath(ENDPOINT_PATH).UsingAnyMethod()).ToList();

        logEntries.Should().HaveCount(1);
        logEntries[0].RequestMessage.Method.Should().BeEquivalentTo("POST",
            because: "the service must use the HTTP POST method");
    }

    [Fact]
    public void Execute_ShouldSendCorrectPayload()
    {
        // Arrange
        StubAddRegEndpoint();
        var cmd = FakeCommand();

        // Act
        _sut.Execute(cmd);

        // Assert
        var logEntries = _server.FindLogEntries(
            Request.Create().WithPath(ENDPOINT_PATH).UsingAnyMethod()).ToList();

        logEntries.Should().HaveCount(1);

        var rawBody = logEntries[0].RequestMessage.Body;
        rawBody.Should().NotBeNullOrEmpty(because: "request body must not be empty");

        using var doc = JsonDocument.Parse(rawBody!);
        var root = doc.RootElement;

        // Field names – RestSharp serialises C# record properties as camelCase by default
        root.GetProperty("regId").GetString().Should().Be(cmd.RegId);
        root.GetProperty("bookingId").GetString().Should().Be(cmd.BookingId);
        root.GetProperty("pasienId").GetString().Should().Be(cmd.PasienId);
        root.GetProperty("pasienName").GetString().Should().Be(cmd.PasienName);
        root.GetProperty("layananId").GetString().Should().Be(cmd.LayananId);
        root.GetProperty("dokterId").GetString().Should().Be(cmd.DokterId);
        root.GetProperty("tglBerobat").GetString().Should().Be(cmd.TglBerobat);
        root.GetProperty("jamJadwal").GetString().Should().Be(cmd.JamJadwal);
        root.GetProperty("noAntrian").GetInt32().Should().Be(cmd.NoAntrian);
    }

    [Fact]
    public void Send_WhenHttpSucceeds_ThenReturnsSuccess()
    {
        StubAddRegEndpoint();
        var result = _sut.Send(FakeCommand());
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Send_WhenBaseUrlIsEmpty_ThenReturnsFailureWithoutHttpCall()
    {
        StubAddRegEndpoint();

        var emptyOpt = Options.Create(new EmrOptions { BaseApiUrl = "" });
        var serviceWithEmptyUrl = new AddAntrianEmrByRegService(emptyOpt);

        var result = serviceWithEmptyUrl.Send(FakeCommand());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EMR BaseApiUrl empty");

        var logEntries = _server.FindLogEntries(
            Request.Create().WithPath(ENDPOINT_PATH).UsingAnyMethod()).ToList();
        logEntries.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenBaseUrlIsEmpty_ShouldNotCallAnyEndpoint()
    {
        // Arrange
        StubAddRegEndpoint();

        var emptyOpt = Options.Create(new EmrOptions { BaseApiUrl = "" });
        var serviceWithEmptyUrl = new AddAntrianEmrByRegService(emptyOpt);
        var cmd = FakeCommand();

        // Act
        serviceWithEmptyUrl.Execute(cmd);

        // Assert
        var logEntries = _server.FindLogEntries(
            Request.Create().WithPath(ENDPOINT_PATH).UsingAnyMethod()).ToList();

        logEntries.Should().BeEmpty(
            because: "when BaseApiUrl is empty the service should return early without making any HTTP call");
    }
}