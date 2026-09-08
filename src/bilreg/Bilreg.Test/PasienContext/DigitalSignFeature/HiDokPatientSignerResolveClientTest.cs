using Bilreg.Application.PasienContext.DigitalSignFeature;
using Bilreg.Infrastructure.PasienContext.DigitalSignFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WireMock.Server;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class HiDokPatientSignerResolveClientTest : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HiDokPatientSignerResolveClient _sut;

    public HiDokPatientSignerResolveClientTest()
    {
        _mockServer = WireMockServer.Start();

        var restClientFactory = new RestClientFactory();
        var optionsWrapper = Options.Create(new HiDokOptions
        {
            BaseApiUrl = _mockServer.Url ?? string.Empty,
            ApiKey = "test-api-key"
        });

        _sut = new HiDokPatientSignerResolveClient(
            optionsWrapper,
            restClientFactory,
            new MemoryCache(new MemoryCacheOptions()));
    }

    public void Dispose()
    {
        _mockServer.Stop();
        _mockServer.Dispose();
    }

    [Fact]
    public void UT01_Given_HiDokReturnsSuccess_When_ExecuteIsCalled_Then_ShouldReturnSuccessWithSigner()
    {
        // Arrange
        var expectedResponse = new
        {
            status = "success",
            code = "200",
            data = new
            {
                UserrId = "U-123",
                SignerId = "SIG-456"
            }
        };

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedResponse));

        var request = new HiDokPatientSignerResolveRequest("1000000", "MR-001");
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert
        result.Status.Should().Be(HiDokPatientSignerResolveStatus.Success);
        result.UserrId.Should().Be("U-123");
        result.SignerId.Should().Be("SIG-456");

        _mockServer.LogEntries.Should().HaveCount(1);
        var logEntry = _mockServer.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("GET");
        logEntry.RequestMessage.Path.Should().Be("/api/digital-sign/patient-signers/resolve");
        logEntry.RequestMessage.Query.Should().ContainKey("hospitalId");
        logEntry.RequestMessage.Query.Should().ContainKey("mr");
        logEntry.RequestMessage.Headers.Should().ContainKey("X-Api-Key");
    }

    [Fact]
    public void UT02_Given_HiDokReturnsNotFound_When_ExecuteIsCalled_Then_ShouldReturnNotFoundStatus()
    {
        // Arrange
        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(404)
                .WithBody("Not Found"));

        // Act
        var result = _sut.Execute(new HiDokPatientSignerResolveRequest("1000000", "MR-001"));

        // Assert
        result.Status.Should().Be(HiDokPatientSignerResolveStatus.NotFound);
        result.SignerId.Should().BeEmpty();
    }

    [Fact]
    public void UT03_Given_HiDokReturnsBadGateway_When_ExecuteIsCalled_Then_ShouldReturnProvisionFailed()
    {
        // Arrange
        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(502)
                .WithBody("Bad Gateway"));

        // Act
        var result = _sut.Execute(new HiDokPatientSignerResolveRequest("1000000", "MR-001"));

        // Assert
        result.Status.Should().Be(HiDokPatientSignerResolveStatus.ProvisionFailed);
        result.SignerId.Should().BeEmpty();
    }

    [Fact]
    public void UT04_Given_HiDokReturnsUnauthorized_When_ExecuteIsCalled_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));

        // Act
        var result = _sut.Execute(new HiDokPatientSignerResolveRequest("1000000", "MR-001"));

        // Assert
        result.Status.Should().Be(HiDokPatientSignerResolveStatus.Unauthorized);
        result.SignerId.Should().BeEmpty();
    }

    [Fact]
    public void UT05_Given_HiDokReturnsError_When_ExecuteIsCalled_Then_ShouldReturnError()
    {
        // Arrange
        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = _sut.Execute(new HiDokPatientSignerResolveRequest("1000000", "MR-001"));

        // Assert
        result.Status.Should().Be(HiDokPatientSignerResolveStatus.Error);
        result.SignerId.Should().BeEmpty();
    }

    [Fact]
    public void UT06_Given_SuccessWasCached_When_ExecuteIsCalledAgain_Then_ShouldNotCallHiDokAgain()
    {
        // Arrange
        var expectedResponse = new
        {
            status = "success",
            code = "200",
            data = new
            {
                UserrId = "U-123",
                SignerId = "SIG-456"
            }
        };

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/digital-sign/patient-signers/resolve")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedResponse));

        var request = new HiDokPatientSignerResolveRequest("1000000", "MR-001");

        // Act
        var first = _sut.Execute(request);
        _mockServer.ResetLogEntries();
        var second = _sut.Execute(request);

        // Assert
        first.Status.Should().Be(HiDokPatientSignerResolveStatus.Success);
        first.SignerId.Should().Be("SIG-456");
        second.Status.Should().Be(HiDokPatientSignerResolveStatus.Success);
        second.SignerId.Should().Be("SIG-456");

        _mockServer.LogEntries.Should().BeEmpty("second resolve should come from in-memory cache");
    }
}