using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class TrackerPharmacyAdapterTest
{
    private const string TrackerId = "01HXYZABCDEFGHJKMNPQRSTVWXY";
    private const string AntrianId = "APT-Q1";

    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly PasienTrackerModel _tracker;
    private readonly AntrianModel _queue;
    private readonly AntrianEntryModel _entry;
    private readonly DateTime _createdAt = new(2025, 8, 3, 8, 0, 0);

    public TrackerPharmacyAdapterTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        _queue = new AntrianModel(
            AntrianId,
            DateOnly.FromDateTime(_createdAt),
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            "tag",
            "Apotek",
            new ServicePointType("APT", "Apotek"),
            [],
            _sequencer.Object);
        var person = new PersonType("Sinta", new DateOnly(2008, 5, 5));
        _tracker = new PasienTrackerModel(
            TrackerId,
            person,
            new DateOnly(2025, 8, 3),
            new DateOnly(2025, 8, 1),
            new DateOnly(2025, 8, 3),
            []);
        _entry = _queue.AddEntry(_tracker, _createdAt);
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(_queue));
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>())).Returns(MayBe.From(_tracker));
    }

    [Fact]
    public void ServeOnce_from_waiting_transitions_queue_and_appends_apotek_start_with_queue_ref()
    {
        var servedAt = _createdAt.AddMinutes(10);
        _antrianRepo.Setup(x => x.TrySaveWaitingToInServiceTransition(_queue, _entry)).Returns(true);
        var sut = CreateSut();

        var result = sut.ServeOnce(AntrianId, 1, TrackerId, "ADP000000001", servedAt);

        result.Applied.Should().BeTrue();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        _entry.ServedAt.Should().Be(servedAt);
        var queueRef = QueueEvidenceReference.Create(AntrianId, 1).Value;
        _tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekStartEventName
            && e.ReffId == queueRef
            && e.EventDate == servedAt);
        _antrianRepo.Verify(x => x.TrySaveWaitingToInServiceTransition(_queue, _entry), Times.Once);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
        _trackerRepo.Verify(x => x.SaveChanges(_tracker), Times.Once);
    }

    [Fact]
    public void ServeOnce_when_already_in_service_is_idempotent_and_does_not_duplicate_evidence()
    {
        var servedAt = _createdAt.AddMinutes(10);
        _entry.Serve(servedAt);
        PharmacyQueueEvidence.AppendApotekStart(
            _tracker,
            QueueEvidenceReference.Create(AntrianId, 1).Value,
            servedAt);
        var sut = CreateSut();

        var result = sut.ServeOnce(AntrianId, 1, TrackerId, "ADP000000002", servedAt.AddMinutes(5));

        result.Applied.Should().BeFalse();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        _tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekStartEventName);
        _antrianRepo.Verify(x => x.TrySaveWaitingToInServiceTransition(
            It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()), Times.Never);
    }

    [Fact]
    public void ServeOnce_when_compare_and_set_fails_throws_concurrency()
    {
        _antrianRepo.Setup(x => x.TrySaveWaitingToInServiceTransition(_queue, _entry)).Returns(false);
        var sut = CreateSut();

        var act = () => sut.ServeOnce(AntrianId, 1, TrackerId, "ADP000000001", _createdAt.AddMinutes(10));

        act.Should().Throw<ApotekConcurrencyException>();
        _tracker.ListEvent.Should().BeEmpty();
        _trackerRepo.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }

    [Fact]
    public void DoneOnce_from_in_service_transitions_queue_and_appends_apotek_done_with_queue_ref()
    {
        var servedAt = _createdAt.AddMinutes(10);
        var doneAt = servedAt.AddMinutes(20);
        _entry.Serve(servedAt);
        _antrianRepo.Setup(x => x.TrySaveInServiceToDoneTransition(_queue, _entry)).Returns(true);
        var sut = CreateSut();

        var result = sut.DoneOnce(AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", doneAt);

        result.Applied.Should().BeTrue();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        _entry.DoneAt.Should().Be(doneAt);
        var queueRef = QueueEvidenceReference.Create(AntrianId, 1).Value;
        _tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekDoneEventName
            && e.ReffId == queueRef
            && e.EventDate == doneAt);
        _antrianRepo.Verify(x => x.TrySaveInServiceToDoneTransition(_queue, _entry), Times.Once);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
    }

    [Fact]
    public void DoneOnce_when_already_done_is_idempotent_and_never_reverses_done()
    {
        var servedAt = _createdAt.AddMinutes(10);
        var doneAt = servedAt.AddMinutes(20);
        _entry.Serve(servedAt);
        _entry.Done(doneAt);
        PharmacyQueueEvidence.AppendApotekDone(
            _tracker,
            QueueEvidenceReference.Create(AntrianId, 1).Value,
            doneAt);
        var sut = CreateSut();

        var result = sut.DoneOnce(AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", doneAt.AddMinutes(5));

        result.Applied.Should().BeFalse();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        _entry.DoneAt.Should().Be(doneAt);
        _tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekDoneEventName);
        _antrianRepo.Verify(x => x.TrySaveInServiceToDoneTransition(
            It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()), Times.Never);
    }

    [Fact]
    public void DoneOnce_pickup_and_noshow_paths_share_queue_done_evidence_ref()
    {
        var servedAt = _createdAt.AddMinutes(10);
        var doneAt = servedAt.AddMinutes(20);
        _entry.Serve(servedAt);
        _antrianRepo.Setup(x => x.TrySaveInServiceToDoneTransition(_queue, _entry)).Returns(true);
        var sut = CreateSut();
        var queueRef = QueueEvidenceReference.Create(AntrianId, 1).Value;

        sut.DoneOnce(AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", doneAt).Applied.Should().BeTrue();
        var replay = sut.DoneOnce(AntrianId, 1, TrackerId, "different-legacy-ref", doneAt.AddMinutes(1));

        replay.Applied.Should().BeFalse();
        _tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekDoneEventName && e.ReffId == queueRef);
    }

    [Fact]
    public void DoneOnce_when_compare_and_set_fails_throws_concurrency()
    {
        _entry.Serve(_createdAt.AddMinutes(10));
        _antrianRepo.Setup(x => x.TrySaveInServiceToDoneTransition(_queue, _entry)).Returns(false);
        var sut = CreateSut();

        var act = () => sut.DoneOnce(AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", _createdAt.AddMinutes(30));

        act.Should().Throw<ApotekConcurrencyException>();
        _tracker.ListEvent.Should().BeEmpty();
        _trackerRepo.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
        _antrianRepo.Verify(x => x.TrySaveInServiceToDoneTransition(_queue, _entry), Times.Once);
    }

    [Fact]
    public void DoneOnce_from_waiting_does_not_append_evidence_or_transition_queue()
    {
        var sut = CreateSut();

        var result = sut.DoneOnce(AntrianId, 1, TrackerId, $"{AntrianId}:1:DONE", _createdAt.AddMinutes(30));

        result.Applied.Should().BeFalse();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        _tracker.ListEvent.Should().BeEmpty();
    }

    [Fact]
    public void WithdrawFromWaiting_when_waiting_withdraws_entry()
    {
        var withdrawnAt = _createdAt.AddMinutes(5);
        var sut = CreateSut();

        var result = sut.WithdrawFromWaiting(AntrianId, 1, "patient left", "closer", withdrawnAt);

        result.Applied.Should().BeTrue();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.Withdrawn);
        _entry.WithdrawalReason.Should().Be("patient left");
        _entry.WithdrawalUserId.Should().Be("closer");
        _antrianRepo.Verify(x => x.SaveChanges(_queue), Times.Once);
    }

    [Fact]
    public void WithdrawFromWaiting_when_not_waiting_returns_not_applied()
    {
        _entry.Serve(_createdAt.AddMinutes(5));
        var sut = CreateSut();

        var result = sut.WithdrawFromWaiting(AntrianId, 1, "late close", "closer", _createdAt.AddMinutes(10));

        result.Applied.Should().BeFalse();
        _entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
    }

    [Fact]
    public void GetStatus_returns_current_queue_entry_status()
    {
        _entry.Serve(_createdAt.AddMinutes(5));
        var sut = CreateSut();

        sut.GetStatus(AntrianId, 1).Should().Be(AntrianStatusEnum.InService);
    }

    private TrackerPharmacyAdapter CreateSut() => new(_antrianRepo.Object, _trackerRepo.Object);
}
