using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
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
    private readonly IAdmissionServicePointResolver _servicePointResolver;

    public TrkJourneyResolveHandlerTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        var servicePoints = new Mock<IAdmissionServicePointRepo>();
        servicePoints.Setup(x => x.LoadEntity(It.IsAny<IAdmissionServicePointKey>()))
            .Returns(MayBe.From(AdmissionServicePointModel.Create("ADM", "Admission", "A")));
        _servicePointResolver = new AdmissionServicePointResolver(servicePoints.Object);
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
        queue.ListEntry.Single().Serve(TestTglJamProvider.Instance.Now);

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
        _antrianRepo
            .Setup(x => x.TrySaveAnonymousInServiceTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Callback<AntrianModel, AntrianEntryModel>((m, _) => savedQueue = m)
            .Returns(true);

        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, _servicePointResolver);
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
            _trackerRepo.Object, _antrianRepo.Object, _servicePointResolver);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("MISSING", "AN001", 1, "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Select_WhenUserIdEmpty_ThenThrows()
    {
        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, _servicePointResolver);
        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd("T1", "AN001", 1, " "),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Select_WhenConditionalUpdateLosesRace_ThenThrowsWithoutSavingTracker()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var person = new PersonType("ANI", new DateOnly(2000, 1, 2));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 5, 3), "BOOKING", "B1", createdAt.AddDays(-1));
        var queue = CreateQueueWithAnonymousEntry("AN003", createdAt);
        queue.ListEntry.Single().Serve(createdAt.AddMinutes(5));
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>()))
            .Returns(MayBe.From(queue));
        _antrianRepo.Setup(x => x.TrySaveAnonymousInServiceTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(false);
        var sut = new TrkJourneyResolveSelectHandler(
            _trackerRepo.Object, _antrianRepo.Object, _servicePointResolver);

        var act = () => sut.Handle(
            new TrkJourneyResolveSelectCmd(tracker.PasienTrackerId, "AN003", 1, "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        _trackerRepo.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }

    private AntrianModel CreateQueueWithAnonymousEntry(string antrianId, DateTime createdAt)
    {
        var queue = new AntrianModel(
            antrianId, DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], _sequencer.Object);
        queue.AddEntry(createdAt);
        return queue;
    }
}
