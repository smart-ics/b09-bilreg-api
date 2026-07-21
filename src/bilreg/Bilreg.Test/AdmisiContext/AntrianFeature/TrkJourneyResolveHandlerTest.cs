using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class TrkJourneyResolveHandlerTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();

    [Fact]
    public async Task Select_WhenTrackerExists_ThenReturnsTrackerId()
    {
        var person = new PersonType("ANI", new DateOnly(2000, 1, 2));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 10, 24), "BOOKING", "B1",
            new DateTime(2025, 10, 10, 9, 0, 0));
        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));

        var sut = new TrkJourneyResolveSelectHandler(_trackerRepo.Object);
        var result = await sut.Handle(
            new TrkJourneyResolveSelectCmd(tracker.PasienTrackerId, "user1"),
            CancellationToken.None);

        result.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
    }

    [Fact]
    public async Task Select_WhenTrackerMissing_ThenThrows()
    {
        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(default(MayBe<PasienTrackerModel>));

        var sut = new TrkJourneyResolveSelectHandler(_trackerRepo.Object);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("MISSING", "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Select_WhenUserIdEmpty_ThenThrows()
    {
        var sut = new TrkJourneyResolveSelectHandler(_trackerRepo.Object);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("T1", " "),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task New_WhenValid_ThenPersistsTrackerWithFirstEvidence()
    {
        PasienTrackerModel? saved = null;
        _trackerRepo
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => saved = m);

        var sut = new TrkJourneyResolveNewHandler(_trackerRepo.Object, TestTglJamProvider.Instance);
        var result = await sut.Handle(
            new TrkJourneyResolveNewCmd(
                "ANI", "2000-01-02", "2025-10-24", "user1", "CHECKIN", "Q-1"),
            CancellationToken.None);

        result.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        saved.Should().NotBeNull();
        saved!.PasienTrackerId.Should().Be(result.PasienTrackerId);
        saved.ListEvent.Should().ContainSingle(e => e.EventName == "CHECKIN" && e.ReffId == "Q-1");
        saved.StartPeriod.Should().Be(DateOnly.FromDateTime(TestTglJamProvider.Instance.Now));
    }
}
