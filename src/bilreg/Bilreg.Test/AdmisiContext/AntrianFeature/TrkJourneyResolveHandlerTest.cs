using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class TrkJourneyResolveHandlerTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<ISequencer> _sequencer = new();

    public TrkJourneyResolveHandlerTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
    }

    [Fact]
    public async Task Select_WhenTrackerAndAnonymousEntry_ThenIdentifiesAndAppendsEvidence()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var person = new PersonType("ANI", new DateOnly(2000, 1, 2));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 10, 24), "BOOKING", "B1",
            new DateTime(2025, 10, 10, 9, 0, 0));
        var queue = CreateQueueWithAnonymousEntry("AN001", createdAt);

        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));
        _antrianRepo
            .Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>()))
            .Returns(MayBe.From(queue));

        PasienTrackerModel? savedTracker = null;
        AntrianModel? savedQueue = null;
        _trackerRepo
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => savedTracker = m);
        _antrianRepo
            .Setup(x => x.SaveChanges(It.IsAny<AntrianModel>()))
            .Callback<AntrianModel>(m => savedQueue = m);

        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, TestTglJamProvider.Instance);
        var result = await sut.Handle(
            new TrkJourneyResolveSelectCmd(tracker.PasienTrackerId, "AN001", 1, "user1"),
            CancellationToken.None);

        result.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        result.AntrianId.Should().Be("AN001");
        result.NoUrut.Should().Be(1);

        var entry = savedQueue!.ListEntry.Single(e => e.NoUrut == 1);
        entry.Tracker.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        entry.ServedAt.Should().Be(TestTglJamProvider.Instance.Now);

        var queueRef = QueueEvidenceReference.Create("AN001", 1).Value;
        savedTracker!.ListEvent.Should().Contain(e =>
            e.EventName == "Check In" && e.ReffId == queueRef && e.EventDate == createdAt);
        savedTracker.ListEvent.Should().Contain(e =>
            e.EventName == "Reg-Start" && e.ReffId == queueRef
            && e.EventDate == TestTglJamProvider.Instance.Now);
    }

    [Fact]
    public async Task Select_WhenTrackerMissing_ThenThrows()
    {
        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(default(MayBe<PasienTrackerModel>));

        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, TestTglJamProvider.Instance);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("MISSING", "AN001", 1, "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Select_WhenUserIdEmpty_ThenThrows()
    {
        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, TestTglJamProvider.Instance);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("T1", "AN001", 1, " "),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task New_WhenValid_ThenCreatesTrackerIdentifiesAndAppendsEvidence()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueueWithAnonymousEntry("AN002", createdAt);

        _antrianRepo
            .Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>()))
            .Returns(MayBe.From(queue));

        PasienTrackerModel? savedTracker = null;
        AntrianModel? savedQueue = null;
        _trackerRepo
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => savedTracker = m);
        _antrianRepo
            .Setup(x => x.SaveChanges(It.IsAny<AntrianModel>()))
            .Callback<AntrianModel>(m => savedQueue = m);

        var sut = new TrkJourneyResolveNewHandler(
            _trackerRepo.Object, _antrianRepo.Object, TestTglJamProvider.Instance);
        var result = await sut.Handle(
            new TrkJourneyResolveNewCmd(
                "ANI", "2000-01-02", "2025-10-24", "AN002", 1, "user1"),
            CancellationToken.None);

        result.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        result.AntrianId.Should().Be("AN002");
        result.NoUrut.Should().Be(1);

        var queueRef = QueueEvidenceReference.Create("AN002", 1).Value;
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().ContainSingle(e =>
            e.EventName == "Check In" && e.ReffId == queueRef && e.EventDate == createdAt);
        savedTracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == "Reg-Start" && e.ReffId == queueRef
            && e.EventDate == TestTglJamProvider.Instance.Now);
        savedTracker.StartPeriod.Should().Be(DateOnly.FromDateTime(createdAt));

        var entry = savedQueue!.ListEntry.Single(e => e.NoUrut == 1);
        entry.Tracker.PasienTrackerId.Should().Be(result.PasienTrackerId);
        entry.Visitor.PersonName.Should().Be("ANI");
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
    }

    private AntrianModel CreateQueueWithAnonymousEntry(string antrianId, DateTime createdAt)
    {
        var queue = new AntrianModel(
            antrianId, DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("Loket", "Loket"), [], _sequencer.Object);
        queue.AddEntry(createdAt);
        return queue;
    }
}
