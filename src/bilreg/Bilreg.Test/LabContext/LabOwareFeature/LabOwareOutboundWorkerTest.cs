using Bilreg.Api.HostedServices;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareOutboundWorkerTest
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<LabOwareOutboundWorker>> _loggerMock = new();

    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepoMock = new();
    private readonly Mock<ILabOrderRepo> _orderRepoMock = new();
    private readonly Mock<ILabOwareIntegration> _integrationMock = new();

    private readonly LabOwareQueueProcessor _processor;

    public LabOwareOutboundWorkerTest()
    {
        _processor = new LabOwareQueueProcessor(
            _queueRepoMock.Object,
            _orderRepoMock.Object,
            _integrationMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(LabOwareQueueProcessor)))
            .Returns(_processor);

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    private static IOptions<OwareOptions> CreateOptions(bool workerEnabled = true, int intervalSeconds = 10)
    {
        return Options.Create(new OwareOptions
        {
            BaseUrl = "http://localhost:5255",
            OrderLabEndpoint = "/api/order/lab/bilreg",
            WorkerEnabled = workerEnabled,
            IntervalSeconds = intervalSeconds
        });
    }

    [Fact]
    public void ProcessBatchOnce_WhenWorkerDisabled_ReturnsNullAndDoesNotCreateScope()
    {
        // Arrange
        var sut = new LabOwareOutboundWorker(
            _scopeFactoryMock.Object,
            CreateOptions(workerEnabled: false),
            _loggerMock.Object);

        // Act
        var result = sut.ProcessBatchOnce();

        // Assert
        result.Should().BeNull();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public void ProcessBatchOnce_WhenWorkerEnabled_OpensScopeAndProcessesBatch()
    {
        // Arrange
        var sut = new LabOwareOutboundWorker(
            _scopeFactoryMock.Object,
            CreateOptions(workerEnabled: true),
            _loggerMock.Object);

        _queueRepoMock
            .Setup(q => q.ListProcessable(LabOwareOutboundWorker.DefaultBatchSize, It.IsAny<int>()))
            .Returns(new List<LabOwareOutboundQueueModel>());

        // Act
        var result = sut.ProcessBatchOnce();

        // Assert
        result.Should().NotBeNull();
        result!.ProcessedCount.Should().Be(0);
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _queueRepoMock.Verify(q => q.ListProcessable(LabOwareOutboundWorker.DefaultBatchSize, 3), Times.Once);
    }

    [Fact]
    public void ProcessBatchOnce_WhenExceptionThrown_CatchesAndReturnsNullWithoutCrashing()
    {
        // Arrange
        var sut = new LabOwareOutboundWorker(
            _scopeFactoryMock.Object,
            CreateOptions(workerEnabled: true),
            _loggerMock.Object);

        _queueRepoMock
            .Setup(q => q.ListProcessable(It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Simulated database failure"));

        // Act
        var result = sut.ProcessBatchOnce();

        // Assert
        result.Should().BeNull();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);

        // Verify warning log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("error while processing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_And_StopAsync_RunsAndTerminatesGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sut = new LabOwareOutboundWorker(
            _scopeFactoryMock.Object,
            CreateOptions(workerEnabled: true, intervalSeconds: 1),
            _loggerMock.Object);

        _queueRepoMock
            .Setup(q => q.ListProcessable(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new List<LabOwareOutboundQueueModel>());

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(50); // allow background loop to start
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.AtLeastOnce);
    }
}
