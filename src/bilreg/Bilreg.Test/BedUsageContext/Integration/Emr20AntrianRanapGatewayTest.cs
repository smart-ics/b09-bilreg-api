using Bilreg.Api.Configurations;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Infrastructure.BedUsageContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bilreg.Test.BedUsageContext.Integration;

public class Emr20AntrianRanapGatewayTest : IDisposable
{
    private readonly WireMockServer _server;
    private readonly Mock<ILogger<Emr20AntrianRanapGateway>> _loggerMock;
    private readonly Emr20AntrianRanapGateway _sut;

    private const string AddRegRanapPath = "/api/Antrian/AddRegRanap";
    private const string AddRanapGabungPath = "/api/Antrian/AddRanapGabung";
    private const string DischargeRegRanapPath = "/api/Antrian/DischargeRegRanap";

    public Emr20AntrianRanapGatewayTest()
    {
        _server = WireMockServer.Start();

        var emr20Options = Options.Create(new Emr20Options
        {
            BaseApiUrl = _server.Urls[0]
        });

        var restClientFactory = new RestClientFactory();
        _loggerMock = new Mock<ILogger<Emr20AntrianRanapGateway>>();

        _sut = new Emr20AntrianRanapGateway(emr20Options, restClientFactory, _loggerMock.Object);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    private async Task<bool> WaitForRequestAsync(int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_server.LogEntries.Any())
                return true;
            await Task.Delay(20);
        }
        return false;
    }

    [Fact]
    public async Task AddRegRanapAsync_WhenCalled_SendsPostRequestWithRegIdQueryParam()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(AddRegRanapPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        // Act
        await _sut.AddRegRanapAsync("REG-RANAP-001");
        var received = await WaitForRequestAsync();

        // Assert
        received.Should().BeTrue("Request should be dispatched to WireMock server within timeout");
        _server.LogEntries.Should().HaveCount(1);

        var logEntry = _server.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("POST");
        logEntry.RequestMessage.Path.Should().Be(AddRegRanapPath);
        logEntry.RequestMessage.Query.Should().ContainKey("regID");
        logEntry.RequestMessage.Query!["regID"].First().Should().Be("REG-RANAP-001");
    }

    [Fact]
    public async Task AddRanapGabungAsync_WhenCalled_SendsPostRequestWithRegIdQueryParam()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(AddRanapGabungPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        // Act
        await _sut.AddRanapGabungAsync("REG-BABY-002");
        var received = await WaitForRequestAsync();

        // Assert
        received.Should().BeTrue("Request should be dispatched to WireMock server within timeout");
        _server.LogEntries.Should().HaveCount(1);

        var logEntry = _server.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("POST");
        logEntry.RequestMessage.Path.Should().Be(AddRanapGabungPath);
        logEntry.RequestMessage.Query.Should().ContainKey("regID");
        logEntry.RequestMessage.Query!["regID"].First().Should().Be("REG-BABY-002");
    }

    [Fact]
    public async Task DischargeRegRanapAsync_WhenCalled_SendsDeleteRequestWithRegIdAndIsGabungFalse()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(DischargeRegRanapPath)
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        // Act
        await _sut.DischargeRegRanapAsync("REG-RANAP-001", isGabung: false);
        var received = await WaitForRequestAsync();

        // Assert
        received.Should().BeTrue("Request should be dispatched to WireMock server within timeout");
        _server.LogEntries.Should().HaveCount(1);

        var logEntry = _server.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("DELETE");
        logEntry.RequestMessage.Path.Should().Be(DischargeRegRanapPath);
        logEntry.RequestMessage.Query.Should().ContainKey("regID");
        logEntry.RequestMessage.Query!["regID"].First().Should().Be("REG-RANAP-001");
        logEntry.RequestMessage.Query.Should().ContainKey("isGabung");
        logEntry.RequestMessage.Query!["isGabung"].First().Should().Be("false");
    }

    [Fact]
    public async Task DischargeRegRanapAsync_WhenCalledWithIsGabungTrue_SendsDeleteRequestWithIsGabungTrue()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(DischargeRegRanapPath)
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        // Act
        await _sut.DischargeRegRanapAsync("REG-BABY-003", isGabung: true);
        var received = await WaitForRequestAsync();

        // Assert
        received.Should().BeTrue();
        var logEntry = _server.LogEntries.First();
        logEntry.RequestMessage.Method.Should().Be("DELETE");
        logEntry.RequestMessage.Query!["regID"].First().Should().Be("REG-BABY-003");
        logEntry.RequestMessage.Query!["isGabung"].First().Should().Be("true");
    }

    [Fact]
    public async Task AddRegRanapAsync_WhenServerReturns500_CompletesSmoothlyWithoutThrowing()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(AddRegRanapPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var act = () => _sut.AddRegRanapAsync("REG-FAIL-500");

        // Assert
        await act.Should().NotThrowAsync();
        var received = await WaitForRequestAsync();
        received.Should().BeTrue();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EMR20 AddRegRanap failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Methods_WhenRestClientFactoryThrowsException_CatchesAndLogsWithoutThrowing()
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        mockFactory.Setup(f => f.Create(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Connection pool exhausted"));

        var loggerMock = new Mock<ILogger<Emr20AntrianRanapGateway>>();
        var sut = new Emr20AntrianRanapGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object,
            loggerMock.Object);

        // Act & Assert for AddRegRanapAsync
        var act1 = () => sut.AddRegRanapAsync("REG-01");
        await act1.Should().NotThrowAsync();

        // Act & Assert for AddRanapGabungAsync
        var act2 = () => sut.AddRanapGabungAsync("REG-02");
        await act2.Should().NotThrowAsync();

        // Act & Assert for DischargeRegRanapAsync
        var act3 = () => sut.DischargeRegRanapAsync("REG-03", false);
        await act3.Should().NotThrowAsync();

        // Wait a short duration for Task.Run threads to complete
        await Task.Delay(150);

        // Verify error logs were recorded for all 3 calls
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Methods_WhenBaseApiUrlIsEmptyOrWhitespace_ShortCircuitsWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var loggerMock = new Mock<ILogger<Emr20AntrianRanapGateway>>();
        var sut = new Emr20AntrianRanapGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object,
            loggerMock.Object);

        // Act
        await sut.AddRegRanapAsync("REG-01");
        await sut.AddRanapGabungAsync("REG-02");
        await sut.DischargeRegRanapAsync("REG-03");

        await Task.Delay(50);

        // Assert
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Methods_WhenRegIdIsEmptyOrWhitespace_ShortCircuitsWithoutHttpCall(string? regId)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var loggerMock = new Mock<ILogger<Emr20AntrianRanapGateway>>();
        var sut = new Emr20AntrianRanapGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object,
            loggerMock.Object);

        // Act
        await sut.AddRegRanapAsync(regId!);
        await sut.AddRanapGabungAsync(regId!);
        await sut.DischargeRegRanapAsync(regId!);

        await Task.Delay(50);

        // Assert
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void AddInfrastructure_RegistersIEmr20AntrianRanapGatewayAndEmr20Options()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Emr20:BaseApiUrl"] = "http://dev.smart-ics.com:8083/emr20-api",
                ["Database:ConnectionString"] = "Server=localhost;Database=test;Trusted_Connection=True;",
                ["BusinessDate:Status"] = "Open"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Verify IEmr20AntrianRanapGateway registration
        var gatewayDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEmr20AntrianRanapGateway));
        gatewayDescriptor.Should().NotBeNull();
        gatewayDescriptor!.ImplementationType.Should().Be(typeof(Emr20AntrianRanapGateway));
        gatewayDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        // Assert - Verify Emr20Options configuration binding
        var sp = services.BuildServiceProvider();
        var emr20Options = sp.GetService<IOptions<Emr20Options>>();
        emr20Options.Should().NotBeNull();
        emr20Options!.Value.BaseApiUrl.Should().Be("http://dev.smart-ics.com:8083/emr20-api");
    }
}
