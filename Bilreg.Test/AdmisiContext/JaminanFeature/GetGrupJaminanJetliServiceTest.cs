using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using WireMock.Server;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class GetGrupJaminanJetliServiceTest : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly GetGrupJaminanJetliService _sut;

    public GetGrupJaminanJetliServiceTest()
    {
        // Arrange: Start WireMock server on a random available port
        _mockServer = WireMockServer.Start();

        // Create a real RestClientFactory that will point to WireMock
        var restClientFactory = new RestClientFactory();

        // Create JetliOptions with the WireMock server URL
        var jetliOptions = new JetliOptions { BaseApiUrl = _mockServer.Url ?? "" };
        var optionsWrapper = Options.Create(jetliOptions);

        // Initialize the service under test with real HTTP layer, but pointing to our stub
        _sut = new GetGrupJaminanJetliService(optionsWrapper, restClientFactory);
    }

    public void Dispose()
    {
        _mockServer.Stop();
        _mockServer.Dispose();
    }

    /// <summary>
    /// UT01: When TipeJaminanId is empty or whitespace, the service should return
    /// the default response ("-", "-", "-") without making any HTTP request.
    /// </summary>
    [Fact]
    public void UT01_Given_EmptyTipeJaminanId_When_ExecuteIsCalled_Then_ShouldReturnDefaultResponseWithoutHttpCall()
    {
        // Arrange
        var request = new GetGrupJaminanJetliRequest("");
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert
        result.Should().NotBeNull();
        result.TipeJaminanId.Should().Be("-");
        result.GroupJaminanId.Should().Be("-");
        result.GroupJaminanName.Should().Be("-");

        // Verify no HTTP request was made
        _mockServer.LogEntries.Should().BeEmpty("service should short-circuit and not call external API");
    }

    /// <summary>
    /// UT01b: When TipeJaminanId contains only whitespace, the service should return
    /// the default response ("-", "-", "-") without making any HTTP request.
    /// </summary>
    [Fact]
    public void UT01b_Given_WhitespaceOnlyTipeJaminanId_When_ExecuteIsCalled_Then_ShouldReturnDefaultResponseWithoutHttpCall()
    {
        // Arrange
        var request = new GetGrupJaminanJetliRequest("   ");
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert
        result.Should().NotBeNull();
        result.TipeJaminanId.Should().Be("-");
        result.GroupJaminanId.Should().Be("-");
        result.GroupJaminanName.Should().Be("-");

        // Verify no HTTP request was made
        _mockServer.LogEntries.Should().BeEmpty("service should short-circuit for whitespace-only input");
    }

    /// <summary>
    /// UT02: When TipeJaminanId is valid and the external API returns HTTP 200,
    /// the service should call GET /api/GrupJaminan/map with the correct query parameter.
    /// </summary>
    [Fact]
    public void UT02_Given_ValidTipeJaminanId_When_ExecuteIsCalled_Then_ShouldCallCorrectEndpointWithGetMethod()
    {
        // Arrange
        const string tipeJaminanId = "JAMINAN123";
        var expectedResponse = new
        {
            status = "success",
            code = "200",
            data = new
            {
                tipeJaminanId,
                groupJaminanId = "GRP001",
                groupJaminanName = "Group Jaminan Pertama"
            }
        };

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/GrupJaminan/map")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedResponse));

        var request = new GetGrupJaminanJetliRequest(tipeJaminanId);
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert - Verify response is correctly deserialized
        result.Should().NotBeNull();
        result.TipeJaminanId.Should().Be(tipeJaminanId);
        result.GroupJaminanId.Should().Be("GRP001");
        result.GroupJaminanName.Should().Be("Group Jaminan Pertama");

        // Assert - Verify HTTP interaction
        _mockServer.LogEntries.Should().HaveCount(1, "exactly one HTTP request should be made");
        var logEntry = _mockServer.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("GET");
        logEntry.RequestMessage.Path.Should().Be("/api/GrupJaminan/map");
    }

    /// <summary>
    /// UT03: When TipeJaminanId is valid, verify the service sends the correct
    /// query parameter "tipeJaminanId" in the request.
    /// </summary>
    [Fact]
    public void UT03_Given_ValidTipeJaminanId_When_ExecuteIsCalled_Then_ShouldSendCorrectQueryParameter()
    {
        // Arrange
        const string tipeJaminanId = "JAMINAN456";
        var expectedResponse = new
        {
            status = "success",
            code = "200",
            data = new
            {
                tipeJaminanId,
                groupJaminanId = "GRP002",
                groupJaminanName = "Group Jaminan Kedua"
            }
        };

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/GrupJaminan/map")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedResponse));

        var request = new GetGrupJaminanJetliRequest(tipeJaminanId);
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert - Verify response is valid (sanity check)
        result.TipeJaminanId.Should().Be(tipeJaminanId);

        // Assert - Verify query parameter was sent correctly
        _mockServer.LogEntries.Should().HaveCount(1);
        var logEntry = _mockServer.LogEntries.First();
        var query = logEntry.RequestMessage.Query;

        query.Should().ContainKey("tipeJaminanId", "query parameter 'tipeJaminanId' should be present");
        var queryValue = query!.FirstOrDefault(x => x.Key == "tipeJaminanId").Value;
        queryValue.Should().NotBeNull().And.NotBeEmpty();
        queryValue!.First().Should().Be(tipeJaminanId, "query parameter value should match the request");
    }

    /// <summary>
    /// UT04: When the external API returns a non-200 status code (e.g., 500),
    /// the service should return the default fallback response ("-", "-", "-").
    /// </summary>
    [Fact]
    public void UT04_Given_ExternalApiReturnsError_When_ExecuteIsCalled_Then_ShouldReturnFallbackResponse()
    {
        // Arrange
        const string tipeJaminanId = "JAMINAN789";

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/GrupJaminan/map")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var request = new GetGrupJaminanJetliRequest(tipeJaminanId);
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert
        result.Should().NotBeNull();
        result.TipeJaminanId.Should().Be("-");
        result.GroupJaminanId.Should().Be("-");
        result.GroupJaminanName.Should().Be("-");

        // Verify the HTTP request was attempted
        _mockServer.LogEntries.Should().HaveCount(1, "HTTP request should have been made");
    }

    /// <summary>
    /// UT05: When the external API returns a 404 Not Found, the service should
    /// return the default fallback response.
    /// </summary>
    [Fact]
    public void UT05_Given_ExternalApiReturnsNotFound_When_ExecuteIsCalled_Then_ShouldReturnFallbackResponse()
    {
        // Arrange
        const string tipeJaminanId = "NONEXISTENT";

        _mockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/GrupJaminan/map")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(404)
                .WithBody("Not Found"));

        var request = new GetGrupJaminanJetliRequest(tipeJaminanId);
        _mockServer.ResetLogEntries();

        // Act
        var result = _sut.Execute(request);

        // Assert
        result.Should().NotBeNull();
        result.TipeJaminanId.Should().Be("-");
        result.GroupJaminanId.Should().Be("-");
        result.GroupJaminanName.Should().Be("-");

        // Verify HTTP request was made
        _mockServer.LogEntries.Should().HaveCount(1, "HTTP request should have been attempted");
    }
}