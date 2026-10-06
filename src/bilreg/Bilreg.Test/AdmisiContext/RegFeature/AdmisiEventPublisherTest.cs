using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using MyHospital.MsgContract.Billing.AdmisiEvents;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class AdmisiEventPublisherTest
{
    private readonly Mock<IBus> _busMock;
    private readonly Mock<ILogger<AdmisiEventPublisher>> _loggerMock;
    private readonly AdmisiEventPublisher _sut;

    public AdmisiEventPublisherTest()
    {
        _busMock = new Mock<IBus>();
        _loggerMock = new Mock<ILogger<AdmisiEventPublisher>>();
        _sut = new AdmisiEventPublisher(_busMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishRajalCreatedAsync_WhenCalled_ShouldPublishRegRajalCreatedNotifEvent()
    {
        // Arrange
        var regId = "REG-RAJAL-001";
        var cancellationToken = new CancellationToken();

        // Act
        await _sut.PublishRajalCreatedAsync(regId, cancellationToken);

        // Assert
        _busMock.Verify(x => x.Publish(
            It.Is<RegRajalCreatedNotifEvent>(e => e.RegId == regId),
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishRajalCreatedAsync_WhenBusThrowsException_ShouldSwallowExceptionAndLogError()
    {
        // Arrange
        var regId = "REG-RAJAL-001";
        var expectedException = new InvalidOperationException("RabbitMQ connection down");
        _busMock.Setup(x => x.Publish(
            It.IsAny<RegRajalCreatedNotifEvent>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = () => _sut.PublishRajalCreatedAsync(regId);

        // Assert
        await act.Should().NotThrowAsync();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task PublishRanapCreatedAsync_WhenCalled_ShouldPublishRegRanapCreatedNotifEvent()
    {
        // Arrange
        var regId = "REG-RANAP-001";
        var cancellationToken = new CancellationToken();

        // Act
        await _sut.PublishRanapCreatedAsync(regId, cancellationToken);

        // Assert
        _busMock.Verify(x => x.Publish(
            It.Is<RegRanapCreatedNotifEvent>(e => e.RegId == regId),
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishRanapCreatedAsync_WhenBusThrowsException_ShouldSwallowExceptionAndLogError()
    {
        // Arrange
        var regId = "REG-RANAP-001";
        var expectedException = new InvalidOperationException("RabbitMQ connection down");
        _busMock.Setup(x => x.Publish(
            It.IsAny<RegRanapCreatedNotifEvent>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = () => _sut.PublishRanapCreatedAsync(regId);

        // Assert
        await act.Should().NotThrowAsync();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
