using Bilreg.Api.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class SignalRPharmacyQueuePublisherTest
{
    [Fact]
    public void Contracts_ExposeStableHubPathAndEventNames()
    {
        PharmacyQueueContracts.HubPath.Should().Be("/hubs/pharmacy-queue");
        PharmacyQueueContracts.NotifyWorklistRefreshEvent.Should().Be("NotifyWorklistRefresh");
        PharmacyQueueContracts.NotifyDisplayRefreshEvent.Should().Be("NotifyDisplayRefresh");
        PharmacyQueueContracts.DepoGroup("DP01").Should().Be("depo:DP01");
        PharmacyQueueContracts.DisplayGroup("DSP01").Should().Be("display:DSP01");
        PharmacyQueueContracts.LoketGroup("LK01").Should().Be("loket:LK01");
    }

    [Fact]
    public void Hints_SupportPropertyAccessAndEquality()
    {
        var worklistHint = new PharmacyWorklistRefreshHint("DEPO-A");
        worklistHint.DepoFarmasiId.Should().Be("DEPO-A");
        worklistHint.DepoId.Should().Be("DEPO-A");

        var displayHint = new PharmacyDisplayRefreshHint("APT-RCV-01", 7);
        displayHint.LoketKey.Should().Be("APT-RCV-01");
        displayHint.AnnouncementVersion.Should().Be(7);
    }

    [Fact]
    public async Task PublishWorklistRefresh_WithDepoId_SendsHintToDepoGroup()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                PharmacyQueueContracts.NotifyWorklistRefreshEvent,
                It.Is<object[]>(args => MatchesWorklistHint(args, "DEPO-A")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(PharmacyQueueContracts.DepoGroup("DEPO-A")))
            .Returns(clientProxy.Object)
            .Verifiable();

        var hubContext = new Mock<IHubContext<PharmacyQueueHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);

        var sut = new SignalRPharmacyQueuePublisher(hubContext.Object, NullLogger<SignalRPharmacyQueuePublisher>.Instance);

        await sut.PublishWorklistRefresh("DEPO-A");

        clients.Verify();
        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishWorklistRefresh_WhenDepoIdEmpty_SendsHintToAll()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                PharmacyQueueContracts.NotifyWorklistRefreshEvent,
                It.Is<object[]>(args => MatchesWorklistHint(args, "")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object).Verifiable();

        var hubContext = new Mock<IHubContext<PharmacyQueueHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);

        var sut = new SignalRPharmacyQueuePublisher(hubContext.Object, NullLogger<SignalRPharmacyQueuePublisher>.Instance);

        await sut.PublishWorklistRefresh("");

        clients.Verify();
        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishWorklistRefresh_WhenTransportFails_SwallowsExceptionAndDoesNotThrow()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR transport error"));

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<PharmacyQueueHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);

        var sut = new SignalRPharmacyQueuePublisher(hubContext.Object, NullLogger<SignalRPharmacyQueuePublisher>.Instance);

        var act = async () => await sut.PublishWorklistRefresh("DEPO-A");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishDisplayRefresh_SendsHintWithLoketKeyAndVersionToAll()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                PharmacyQueueContracts.NotifyDisplayRefreshEvent,
                It.Is<object[]>(args => MatchesDisplayHint(args, "APT-RCV-01", 12)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object).Verifiable();

        var hubContext = new Mock<IHubContext<PharmacyQueueHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);

        var sut = new SignalRPharmacyQueuePublisher(hubContext.Object, NullLogger<SignalRPharmacyQueuePublisher>.Instance);

        await sut.PublishDisplayRefresh("APT-RCV-01", 12);

        clients.Verify();
        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishDisplayRefresh_WhenTransportFails_SwallowsExceptionAndDoesNotThrow()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR transport error"));

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<PharmacyQueueHub>>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);

        var sut = new SignalRPharmacyQueuePublisher(hubContext.Object, NullLogger<SignalRPharmacyQueuePublisher>.Instance);

        var act = async () => await sut.PublishDisplayRefresh("APT-RCV-01", 12);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PharmacyQueueHub_SubscribeAndUnsubscribeDepo_AddsAndRemovesGroup()
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(x => x.AddToGroupAsync("conn-1", "depo:DEPO-01", default))
            .Returns(Task.CompletedTask)
            .Verifiable();
        groups.Setup(x => x.RemoveFromGroupAsync("conn-1", "depo:DEPO-01", default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var context = new Mock<HubCallerContext>();
        context.Setup(x => x.ConnectionId).Returns("conn-1");

        var hub = new PharmacyQueueHub
        {
            Groups = groups.Object,
            Context = context.Object
        };

        await hub.SubscribeDepo("DEPO-01");
        await hub.UnsubscribeDepo("DEPO-01");

        // Test alias methods as well
        await hub.JoinDepoGroup("DEPO-01");
        await hub.LeaveDepoGroup("DEPO-01");

        groups.Verify(x => x.AddToGroupAsync("conn-1", "depo:DEPO-01", default), Times.Exactly(2));
        groups.Verify(x => x.RemoveFromGroupAsync("conn-1", "depo:DEPO-01", default), Times.Exactly(2));
    }

    [Fact]
    public async Task PharmacyQueueHub_SubscribeAndUnsubscribeDisplay_AddsAndRemovesGroup()
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(x => x.AddToGroupAsync("conn-2", "display:DISP-APT", default))
            .Returns(Task.CompletedTask)
            .Verifiable();
        groups.Setup(x => x.RemoveFromGroupAsync("conn-2", "display:DISP-APT", default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var context = new Mock<HubCallerContext>();
        context.Setup(x => x.ConnectionId).Returns("conn-2");

        var hub = new PharmacyQueueHub
        {
            Groups = groups.Object,
            Context = context.Object
        };

        await hub.SubscribeDisplay("DISP-APT");
        await hub.UnsubscribeDisplay("DISP-APT");

        // Test alias methods as well
        await hub.JoinDisplayGroup("DISP-APT");
        await hub.LeaveDisplayGroup("DISP-APT");

        groups.Verify(x => x.AddToGroupAsync("conn-2", "display:DISP-APT", default), Times.Exactly(2));
        groups.Verify(x => x.RemoveFromGroupAsync("conn-2", "display:DISP-APT", default), Times.Exactly(2));
    }

    [Fact]
    public async Task PharmacyQueueHub_OnConnectedAsync_WithQueryParameters_RegistersGroups()
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(x => x.AddToGroupAsync("conn-3", "depo:DEPO-RJ", default))
            .Returns(Task.CompletedTask)
            .Verifiable();
        groups.Setup(x => x.AddToGroupAsync("conn-3", "display:DISP-01", default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["depoId"] = "DEPO-RJ",
            ["displayId"] = "DISP-01"
        });

        var context = new Mock<HubCallerContext>();
        context.Setup(x => x.ConnectionId).Returns("conn-3");

        var hub = new TestPharmacyQueueHub(httpContext)
        {
            Groups = groups.Object,
            Context = context.Object
        };

        await hub.OnConnectedAsync();

        groups.Verify(x => x.AddToGroupAsync("conn-3", "depo:DEPO-RJ", default), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("conn-3", "display:DISP-01", default), Times.Once);
    }

    [Fact]
    public async Task PharmacyQueueHub_NotifyWorklistRefresh_BroadcastsToGroupOrAll()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                PharmacyQueueContracts.NotifyWorklistRefreshEvent,
                It.Is<object[]>(args => MatchesWorklistHint(args, "DEPO-B")),
                default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubCallerClients>();
        clients.Setup(x => x.Group("depo:DEPO-B")).Returns(clientProxy.Object).Verifiable();

        var hub = new PharmacyQueueHub
        {
            Clients = clients.Object
        };

        await hub.NotifyWorklistRefresh("DEPO-B");

        clients.Verify();
        clientProxy.Verify();
    }

    [Fact]
    public async Task PharmacyQueueHub_NotifyDisplayRefresh_BroadcastsToAll()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                PharmacyQueueContracts.NotifyDisplayRefreshEvent,
                It.Is<object[]>(args => MatchesDisplayHint(args, "APT-SRH-01", 3)),
                default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubCallerClients>();
        clients.Setup(x => x.All).Returns(clientProxy.Object).Verifiable();

        var hub = new PharmacyQueueHub
        {
            Clients = clients.Object
        };

        await hub.NotifyDisplayRefresh("APT-SRH-01", 3);

        clients.Verify();
        clientProxy.Verify();
    }

    private static bool MatchesWorklistHint(object[] args, string expectedDepoId)
    {
        if (args.Length != 1) return false;
        return args[0] is PharmacyWorklistRefreshHint hint && hint.DepoFarmasiId == expectedDepoId;
    }

    private static bool MatchesDisplayHint(object[] args, string expectedLoketKey, long expectedVersion)
    {
        if (args.Length != 1) return false;
        return args[0] is PharmacyDisplayRefreshHint hint &&
               hint.LoketKey == expectedLoketKey &&
               hint.AnnouncementVersion == expectedVersion;
    }

    private sealed class TestPharmacyQueueHub(HttpContext httpContext) : PharmacyQueueHub
    {
        protected override HttpContext? GetCallerHttpContext() => httpContext;
    }
}
