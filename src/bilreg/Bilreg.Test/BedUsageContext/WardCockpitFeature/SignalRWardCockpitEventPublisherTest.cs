using Bilreg.Api.SignalR;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class SignalRWardCockpitEventPublisherTest
{
    [Fact]
    public void Contracts_AreStableAndCorrect()
    {
        WardCockpitContracts.HubPath.Should().Be("/hubs/ward-cockpit");
        WardCockpitContracts.GroupPrefix.Should().Be("Ward-");
        WardCockpitContracts.OnBedStatusChangedEvent.Should().Be("OnBedStatusChanged");
        WardCockpitContracts.OnWaitingListUpdatedEvent.Should().Be("OnWaitingListUpdated");
        WardCockpitContracts.OnKpiUpdatedEvent.Should().Be("OnKpiUpdated");
        WardCockpitContracts.GetWardGroupName("BGS-01").Should().Be("Ward-BGS-01");
    }

    [Fact]
    public async Task PublishBedStatusChangedAsync_SendsPayloadToWardGroup()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                WardCockpitContracts.OnBedStatusChangedEvent,
                It.Is<object[]>(args => MatchesBedStatus(args, "BED-01", "Occupied")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut("Ward-BGS-01", clientProxy);

        await sut.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", new { RegId = "REG-01" }, CancellationToken.None);

        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishWaitingListUpdatedAsync_SendsPayloadToWardGroup()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                WardCockpitContracts.OnWaitingListUpdatedEvent,
                It.Is<object[]>(args => MatchesWaitingList(args, "BGS-01")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut("Ward-BGS-01", clientProxy);

        await sut.PublishWaitingListUpdatedAsync("BGS-01", CancellationToken.None);

        clientProxy.Verify();
    }

    [Fact]
    public async Task PublishKpiUpdatedAsync_SendsPayloadToWardGroup()
    {
        var clientProxy = new Mock<IClientProxy>();
        var kpi = new WardCockpitKpiDto(20, 15, 75.0, 3, 2, 0, 1);
        clientProxy
            .Setup(x => x.SendCoreAsync(
                WardCockpitContracts.OnKpiUpdatedEvent,
                It.Is<object[]>(args => MatchesKpi(args)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = CreateSut("Ward-BGS-01", clientProxy);

        await sut.PublishKpiUpdatedAsync("BGS-01", kpi, CancellationToken.None);

        clientProxy.Verify();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Publish_WhenBangsalIdNullOrWhitespace_DoesNotSend(string? invalidBangsalId)
    {
        var clientProxy = new Mock<IClientProxy>();
        var sut = CreateSut("Ward-BGS-01", clientProxy);

        await sut.PublishBedStatusChangedAsync(invalidBangsalId!, "BED-01", "Occupied");
        await sut.PublishWaitingListUpdatedAsync(invalidBangsalId!);
        await sut.PublishKpiUpdatedAsync(invalidBangsalId!, new { });

        clientProxy.Verify(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WhenTransportFails_LogsWarningAndDoesNotThrow()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connection drop"));

        var sut = CreateSut("Ward-BGS-01", clientProxy);

        var act1 = async () => await sut.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied");
        var act2 = async () => await sut.PublishWaitingListUpdatedAsync("BGS-01");
        var act3 = async () => await sut.PublishKpiUpdatedAsync("BGS-01", new { });

        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
        await act3.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NullWardCockpitEventPublisher_ExecutesSafely()
    {
        IWardCockpitEventPublisher sut = new NullWardCockpitEventPublisher();

        var act1 = async () => await sut.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied");
        var act2 = async () => await sut.PublishWaitingListUpdatedAsync("BGS-01");
        var act3 = async () => await sut.PublishKpiUpdatedAsync("BGS-01", new { });
        var act4 = async () => await sut.OnBedStatusChanged("BGS-01", "BED-01", "Occupied");
        var act5 = async () => await sut.OnWaitingListUpdated("BGS-01");
        var act6 = async () => await sut.OnKpiUpdated("BGS-01", new { });

        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
        await act3.Should().NotThrowAsync();
        await act4.Should().NotThrowAsync();
        await act5.Should().NotThrowAsync();
        await act6.Should().NotThrowAsync();
    }

    private static bool MatchesBedStatus(object[] args, string expectedBedId, string expectedStatus)
    {
        if (args.Length != 1) return false;
        var payload = args[0] as BedStatusChangedPayload;
        return payload != null && payload.BedId == expectedBedId && payload.NewStatus == expectedStatus;
    }

    private static bool MatchesWaitingList(object[] args, string expectedBangsalId)
    {
        if (args.Length != 1) return false;
        var payload = args[0] as WaitingListUpdatedPayload;
        return payload != null && payload.BangsalId == expectedBangsalId;
    }

    private static bool MatchesKpi(object[] args)
    {
        if (args.Length != 1) return false;
        var payload = args[0] as KpiUpdatedPayload;
        return payload?.Kpi != null;
    }

    private static SignalRWardCockpitEventPublisher CreateSut(string expectedGroup, Mock<IClientProxy> clientProxy)
    {
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(x => x.Group(expectedGroup)).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<WardCockpitHub>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);

        return new SignalRWardCockpitEventPublisher(
            hubContext.Object,
            NullLogger<SignalRWardCockpitEventPublisher>.Instance);
    }
}
