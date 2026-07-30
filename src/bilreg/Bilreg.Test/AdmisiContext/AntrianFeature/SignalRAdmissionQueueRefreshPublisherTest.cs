using Bilreg.Api.SignalR;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class SignalRAdmissionQueueRefreshPublisherTest
{
    [Fact]
    public void RefreshContract_IsStable()
    {
        AdmissionQueueRefreshContracts.HubPath.Should().Be("/hubs/admission-queue");
        AdmissionQueueRefreshContracts.RefreshHintEvent.Should().Be("RefreshHint");
    }

    [Fact]
    public async Task PublishAsync_SendsRefreshHintWithLoketKey()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                AdmissionQueueRefreshContracts.RefreshHintEvent,
                It.Is<object[]>(args => MatchesHint(args, "L1")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut(clientProxy);

        await sut.PublishAsync("L1", CancellationToken.None);

        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishAsync_WhenLoketKeyNull_SendsNullHint()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                AdmissionQueueRefreshContracts.RefreshHintEvent,
                It.Is<object[]>(args => MatchesHint(args, null)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut(clientProxy);

        await sut.PublishAsync(null, CancellationToken.None);

        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishAsync_WhenTransportFails_DoesNotThrow()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport unavailable"));

        var sut = CreateSut(clientProxy);

        var act = async () => await sut.PublishAsync("L1", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static bool MatchesHint(object[] args, string? expectedLoketKey)
    {
        if (args.Length != 1)
            return false;
        var hint = args[0] as AdmissionQueueRefreshHint;
        return hint is not null && hint.LoketKey == expectedLoketKey;
    }

    private static SignalRAdmissionQueueRefreshPublisher CreateSut(Mock<IClientProxy> clientProxy)
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<AdmissionQueueRefreshHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);
        return new SignalRAdmissionQueueRefreshPublisher(
            hubContext.Object,
            NullLogger<SignalRAdmissionQueueRefreshPublisher>.Instance);
    }
}
