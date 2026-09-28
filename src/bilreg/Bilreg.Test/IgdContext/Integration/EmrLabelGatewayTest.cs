using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Infrastructure.IgdContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bilreg.Test.IgdContext.Integration;

public class EmrLabelGatewayTest : IDisposable
{
    private readonly WireMockServer _server;
    private readonly EmrLabelGateway _sut;
    private const string AddSmassPath = "/api/LabelV2/AddSmass";

    public EmrLabelGatewayTest()
    {
        _server = WireMockServer.Start();

        var emr20Options = Options.Create(new Emr20Options
        {
            BaseApiUrl = _server.Urls[0]
        });

        var restClientFactory = new RestClientFactory();

        _sut = new EmrLabelGateway(emr20Options, restClientFactory);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task AddSmassLabel_WhenSuccessful_ReturnsSuccessResultAndCorrectPayload()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(AddSmassPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        var request = new EmrAddSmassLabelRequest
        {
            AssesmentId = "ASM-001",
            LayananId = "1GD01",
            PaperId = "PP-ICS-TRGE",
            PaperName = "FORMULIR TRIASE IGD",
            RegId = "REG-2026-0001",
            UserrId = "USR-NURSE-01"
        };

        // Act
        var result = await _sut.AddSmassLabel(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        var logEntries = _server.LogEntries;
        logEntries.Should().HaveCount(1);
        var receivedBody = logEntries.First().RequestMessage.Body;
        receivedBody.Should().Contain("\"AssesmentId\":\"ASM-001\"");
        receivedBody.Should().Contain("\"LayananId\":\"1GD01\"");
        receivedBody.Should().Contain("\"PaperId\":\"PP-ICS-TRGE\"");
        receivedBody.Should().Contain("\"PaperName\":\"FORMULIR TRIASE IGD\"");
        receivedBody.Should().Contain("\"RegId\":\"REG-2026-0001\"");
        receivedBody.Should().Contain("\"UserrId\":\"USR-NURSE-01\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AddSmassLabel_WhenBaseApiUrlEmptyOrWhitespace_ReturnsFailureWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new EmrLabelGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object);

        var request = new EmrAddSmassLabelRequest
        {
            AssesmentId = "ASM-001",
            RegId = "REG-001"
        };

        // Act
        var result = await sut.AddSmassLabel(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Konfigurasi Emr20:BaseApiUrl belum diisi.");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddSmassLabel_WhenRequestIsNull_ReturnsFailureWithoutHttpCall()
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new EmrLabelGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act
        var result = await sut.AddSmassLabel(null!);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payload AddSmass kosong.");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddSmassLabel_WhenHttpReturnsNonSuccess_ReturnsFailureWithDetails()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(AddSmassPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var request = new EmrAddSmassLabelRequest
        {
            AssesmentId = "ASM-001",
            RegId = "REG-001"
        };

        // Act
        var result = await _sut.AddSmassLabel(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task AddSmassLabel_WhenExceptionThrown_CatchesExceptionAndReturnsFailure()
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        mockFactory
            .Setup(f => f.Create(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Connection pool exhausted"));

        var sut = new EmrLabelGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        var request = new EmrAddSmassLabelRequest
        {
            AssesmentId = "ASM-001",
            RegId = "REG-001"
        };

        // Act
        var result = await sut.AddSmassLabel(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection pool exhausted");
    }

    [Fact]
    public void AddInfrastructure_RegistersIEmrLabelGatewayAndEmr20Options()
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

        // Assert - Verify IEmrLabelGateway service registration
        var gatewayDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEmrLabelGateway));
        gatewayDescriptor.Should().NotBeNull();
        gatewayDescriptor!.ImplementationType.Should().Be(typeof(EmrLabelGateway));
        gatewayDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        // Assert - Verify Emr20Options configuration binding
        var sp = services.BuildServiceProvider();
        var emr20Options = sp.GetService<IOptions<Emr20Options>>();
        emr20Options.Should().NotBeNull();
        emr20Options!.Value.BaseApiUrl.Should().Be("http://dev.smart-ics.com:8083/emr20-api");
    }
}
