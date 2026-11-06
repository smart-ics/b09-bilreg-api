using Bilreg.Application.Shared;
using Bilreg.Application.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.Shared.Helpers;

public class SimulationTimeGuardTest
{
    private readonly Mock<ITglJamProvider> _providerMock;
    private readonly Mock<IBusinessDateStatus> _statusMock;
    private readonly Mock<ILogger> _loggerMock;

    public SimulationTimeGuardTest()
    {
        _providerMock = new Mock<ITglJamProvider>();
        _statusMock = _providerMock.As<IBusinessDateStatus>();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Resolve_WhenRequestTimeIsNull_ReturnsProviderNow()
    {
        // Arrange
        var now = new DateTime(2025, 5, 3, 10, 30, 0);
        _providerMock.Setup(p => p.Now).Returns(now);

        // Act
        var result = SimulationTimeGuard.Resolve(null, _providerMock.Object);

        // Assert
        result.Should().Be(now);
    }

    [Fact]
    public void Resolve_WhenRequestTimeIsDefault_ReturnsProviderNow()
    {
        // Arrange
        var now = new DateTime(2025, 5, 3, 10, 30, 0);
        _providerMock.Setup(p => p.Now).Returns(now);

        // Act
        var result = SimulationTimeGuard.Resolve(default(DateTime), _providerMock.Object);

        // Assert
        result.Should().Be(now);
    }

    [Fact]
    public void Resolve_WhenNotSimulation_ReturnsClientRequestTime()
    {
        // Arrange
        var clientTime = new DateTime(2026, 10, 29, 9, 0, 0);
        var providerNow = new DateTime(2026, 10, 29, 9, 5, 0);
        _providerMock.Setup(p => p.Now).Returns(providerNow);
        _statusMock.Setup(s => s.IsSimulation).Returns(false);

        // Act
        var result = SimulationTimeGuard.Resolve(clientTime, _providerMock.Object);

        // Assert
        result.Should().Be(clientTime);
    }

    [Fact]
    public void Resolve_WhenSimulationAndDateDiffers_AutoCorrectsToProviderNow()
    {
        // Arrange (Client kirim waktu komputer 2026-10-29, tapi mode simulasi di 2025-05-03)
        var clientTime = new DateTime(2026, 10, 29, 9, 0, 0);
        var simBusinessNow = new DateTime(2025, 5, 3, 10, 30, 0);
        var simFixedDate = new DateOnly(2025, 5, 3);

        _providerMock.Setup(p => p.Now).Returns(simBusinessNow);
        _statusMock.Setup(s => s.IsSimulation).Returns(true);
        _statusMock.Setup(s => s.FixedDate).Returns(simFixedDate);

        // Act
        var result = SimulationTimeGuard.Resolve(
            clientTime,
            _providerMock.Object,
            _loggerMock.Object,
            operationName: "UnitTestCheckIn");

        // Assert
        result.Should().Be(simBusinessNow);
    }

    [Fact]
    public void Resolve_WhenSimulationAndDateMatches_KeepsClientTime()
    {
        // Arrange (Client kirim waktu dengan tanggal yang sama dengan tanggal simulasi)
        var clientTime = new DateTime(2025, 5, 3, 8, 15, 0);
        var simBusinessNow = new DateTime(2025, 5, 3, 10, 30, 0);
        var simFixedDate = new DateOnly(2025, 5, 3);

        _providerMock.Setup(p => p.Now).Returns(simBusinessNow);
        _statusMock.Setup(s => s.IsSimulation).Returns(true);
        _statusMock.Setup(s => s.FixedDate).Returns(simFixedDate);

        // Act
        var result = SimulationTimeGuard.Resolve(
            clientTime,
            _providerMock.Object,
            _loggerMock.Object,
            operationName: "UnitTestCheckIn");

        // Assert
        result.Should().Be(clientTime);
    }
}
