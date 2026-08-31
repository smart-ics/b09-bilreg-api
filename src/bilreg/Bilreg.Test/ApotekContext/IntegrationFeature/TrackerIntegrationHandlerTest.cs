using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class TrackerIntegrationHandlerTest
{
    private const string AntrianId = "APT-Q1";
    private const string TrackerId = "01HXYZABCDEFGHJKMNPQRSTVWXY";

    [Fact]
    public void TrackerServedAtHandler_calls_serve_once_with_payload_identity()
    {
        var tracker = new Mock<ITrackerPharmacyPort>();
        tracker.Setup(x => x.ServeOnce(AntrianId, 1, TrackerId, "ADP000000001", It.IsAny<DateTime>()))
            .Returns(new TrackerPharmacyCommandResult(true, "corr"));
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000001",
            "Q1:1:SERVE",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                AntrianId,
                NoUrut = 1,
                PasienTrackerId = TrackerId,
                ReffId = "ADP000000001"
            }));
        var sut = new TrackerServedAtHandler(tracker.Object);

        var result = sut.Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr");
        tracker.Verify(x => x.ServeOnce(AntrianId, 1, TrackerId, "ADP000000001", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public void TrackerDoneAtPickup_and_NoShow_handlers_share_done_idempotency_key()
    {
        var tracker = new Mock<ITrackerPharmacyPort>();
        tracker.Setup(x => x.DoneOnce(
                AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", It.IsAny<DateTime>()))
            .Returns(new TrackerPharmacyCommandResult(true, "done-corr"));
        var sharedKey = $"{AntrianId}:1:DONE";
        var payload = $$"""{"AntrianId":"{{AntrianId}}","NoUrut":1,"PasienTrackerId":"{{TrackerId}}"}""";
        var pickupTask = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerDoneAtPickup,
            AptIntegrationSourceKindEnum.Mapping,
            $"{AntrianId}:1",
            sharedKey,
            AptIntegrationDestinationEnum.Tracker,
            payload);
        var noShowTask = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerDoneAtNoShow,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000002",
            sharedKey,
            AptIntegrationDestinationEnum.Tracker,
            payload);

        new TrackerDoneAtPickupHandler(tracker.Object).Handle(pickupTask).Success.Should().BeTrue();
        new TrackerDoneAtNoShowHandler(tracker.Object).Handle(noShowTask).Success.Should().BeTrue();

        tracker.Verify(x => x.DoneOnce(
            AntrianId, 1, TrackerId, sharedKey, It.IsAny<DateTime>()), Times.Exactly(2));
    }

    [Fact]
    public void TrackerWithdrawnHandler_returns_failure_when_withdraw_not_applied()
    {
        var tracker = new Mock<ITrackerPharmacyPort>();
        tracker.Setup(x => x.WithdrawFromWaiting(AntrianId, 1, "left", "closer", It.IsAny<DateTime>()))
            .Returns(new TrackerPharmacyCommandResult(false, ""));
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerWithdrawn,
            AptIntegrationSourceKindEnum.QueueClose,
            "QC000000001",
            "QC000000001:WDN",
            AptIntegrationDestinationEnum.Tracker,
            $$"""{"AntrianId":"{{AntrianId}}","NoUrut":1,"Reason":"left","UserId":"closer"}""");
        var sut = new TrackerWithdrawnHandler(tracker.Object);

        var result = sut.Handle(task);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("withdraw failed");
    }

    [Fact]
    public void TrackerWithdrawnHandler_succeeds_when_withdraw_applied()
    {
        var tracker = new Mock<ITrackerPharmacyPort>();
        tracker.Setup(x => x.WithdrawFromWaiting(AntrianId, 1, "left", "closer", It.IsAny<DateTime>()))
            .Returns(new TrackerPharmacyCommandResult(true, "wdn-corr"));
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerWithdrawn,
            AptIntegrationSourceKindEnum.QueueClose,
            "QC000000001",
            "QC000000001:WDN",
            AptIntegrationDestinationEnum.Tracker,
            $$"""{"AntrianId":"{{AntrianId}}","NoUrut":1,"Reason":"left","UserId":"closer"}""");
        var sut = new TrackerWithdrawnHandler(tracker.Object);

        var result = sut.Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("wdn-corr");
    }
}
