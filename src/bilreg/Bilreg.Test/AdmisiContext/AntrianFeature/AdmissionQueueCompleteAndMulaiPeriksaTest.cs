using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueCompleteTest
{
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<ISequencer> _sequencer = new();

    public AdmissionQueueCompleteTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(7);
    }

    [Fact]
    public void CreateOnReg_WhenNoAdmissionKeys_ThenCreatesIdentifiedDoneEntryAndRegisterEvidence()
    {
        var occurredAt = TestTglJamProvider.Instance.Now;
        var businessDate = DateOnly.FromDateTime(occurredAt);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, businessDate, "BOOKING", "BK1",
            occurredAt.AddDays(-1));
        var admissionQueue = new AntrianFactory(_sequencer.Object)
            .Create(new ServicePointType("ADM", "Loket Admisi"), businessDate);

        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);
        _antrianFactory
            .Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate))
            .Returns(admissionQueue);

        var result = AdmissionQueueComplete.CompleteAtRegistration(
            _antrianRepo.Object, _antrianFactory.Object, tracker, "RG001", occurredAt,
            null, null);

        result.Should().BeSameAs(admissionQueue);
        var entry = result.ListEntry.Single(e => e.NoUrut == 7);
        entry.Tracker.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        entry.CreatedAt.Should().Be(occurredAt);
        entry.ServedAt.Should().Be(occurredAt);
        entry.DoneAt.Should().Be(occurredAt);
        tracker.ListEvent.Should().Contain(e =>
            e.EventName == "REGISTER" && e.ReffId == "RG001" && e.EventDate == occurredAt);
    }

    [Fact]
    public void ExistingAdmission_WhenInService_ThenDoneWithoutReServe()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var servedAt = new DateTime(2025, 5, 3, 8, 10, 0);
        var doneAt = new DateTime(2025, 5, 3, 8, 25, 0);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(createdAt), "BOOKING", "BK1", createdAt);

        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], _sequencer.Object);
        var entry = queue.AddEntry(tracker, createdAt);
        entry.Serve(servedAt);

        _antrianRepo
            .Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>()))
            .Returns(MayBe.From(queue));

        AdmissionQueueComplete.CompleteAtRegistration(
            _antrianRepo.Object, _antrianFactory.Object, tracker, "RG002", doneAt,
            "ADM-Q1", entry.NoUrut);

        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        entry.ServedAt.Should().Be(servedAt);
        entry.DoneAt.Should().Be(doneAt);
        tracker.ListEvent.Should().Contain(e =>
            e.EventName == "REGISTER" && e.ReffId == "RG002");
    }

    [Fact]
    public void AppendRegister_WhenAlreadyPresent_ThenDoesNotDuplicate()
    {
        var occurredAt = TestTglJamProvider.Instance.Now;
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(occurredAt), "REGISTER", "RG003", occurredAt);

        AdmissionQueueComplete.AppendRegisterIfMissing(tracker, "RG003", occurredAt);

        tracker.ListEvent.Count(e => e.EventName == "REGISTER" && e.ReffId == "RG003")
            .Should().Be(1);
    }
}

public class QueMulaiPeriksaHandlerTest
{
    private readonly Mock<IAntrianRepo> _queRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<ISequencer> _sequencer = new();

    public QueMulaiPeriksaHandlerTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(15);
    }

    [Fact]
    public async Task MulaiPeriksa_WhenWaiting_ThenInServiceAndConsultStartEvidence()
    {
        var createdAt = new DateTime(2025, 5, 3, 7, 0, 0);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(createdAt), "BOOKING", "BK1", createdAt);
        var queue = new AntrianModel(
            "DOC-Q1", DateOnly.FromDateTime(createdAt), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag-doc", "Praktek", new ServicePointType("DR1", "Dokter"), [], _sequencer.Object);
        queue.AddEntry(15, tracker, "RG1", "REG", createdAt);

        _queRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
        _trackerRepo.Setup(x => x.LoadEntity(tracker)).Returns(MayBe.From(tracker));

        AntrianModel? saved = null;
        _queRepo.Setup(x => x.SaveChanges(It.IsAny<AntrianModel>()))
            .Callback<AntrianModel>(m => saved = m);
        _trackerRepo.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()));

        var sut = new QueMulaiPeriksaHandler(
            _queRepo.Object, _trackerRepo.Object, TestTglJamProvider.Instance);
        var response = await sut.Handle(new QueMulaiPeriksaCmd("DOC-Q1", 15), CancellationToken.None);

        response.AntrianId.Should().Be("DOC-Q1");
        response.NoUrut.Should().Be(15);
        response.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        response.Status.Should().Be("InService");

        var entry = saved!.ListEntry.Single(e => e.NoUrut == 15);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        entry.ServedAt.Should().Be(TestTglJamProvider.Instance.Now);
        tracker.ListEvent.Should().Contain(e =>
            e.EventName == PhysicianQueueEvidence.ConsultStartEventName
            && e.ReffId == "DOC-Q1/No.15"
            && e.EventDate == TestTglJamProvider.Instance.Now);
    }

    [Fact]
    public async Task SelesaiPeriksa_AfterMulai_ThenDoneAndConsultDoneEvidence()
    {
        var createdAt = new DateTime(2025, 5, 3, 7, 0, 0);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(createdAt), "BOOKING", "BK1", createdAt);
        var queue = new AntrianModel(
            "DOC-Q1", DateOnly.FromDateTime(createdAt), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag-doc", "Praktek", new ServicePointType("DR1", "Dokter"), [], _sequencer.Object);
        queue.AddEntry(15, tracker, "RG1", "REG", createdAt);

        _queRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
        _trackerRepo.Setup(x => x.LoadEntity(tracker)).Returns(MayBe.From(tracker));
        _queRepo.Setup(x => x.SaveChanges(It.IsAny<AntrianModel>()));
        _trackerRepo.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()));

        var mulai = new QueMulaiPeriksaHandler(
            _queRepo.Object, _trackerRepo.Object, TestTglJamProvider.Instance);
        var mulaiResponse = await mulai.Handle(new QueMulaiPeriksaCmd("DOC-Q1", 15), CancellationToken.None);
        mulaiResponse.Status.Should().Be("InService");
        mulaiResponse.PasienTrackerId.Should().Be(tracker.PasienTrackerId);

        var selesai = new QueSelesaiPeriksaHandler(
            _queRepo.Object, _trackerRepo.Object, TestTglJamProvider.Instance);
        var selesaiResponse = await selesai.Handle(new QueSelesaiPeriksaCmd("DOC-Q1", 15), CancellationToken.None);
        selesaiResponse.Status.Should().Be("Done");
        selesaiResponse.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        selesaiResponse.AntrianId.Should().Be("DOC-Q1");
        selesaiResponse.NoUrut.Should().Be(15);

        var entry = queue.ListEntry.Single(e => e.NoUrut == 15);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        entry.DoneAt.Should().Be(TestTglJamProvider.Instance.Now);
        tracker.ListEvent.Should().Contain(e =>
            e.EventName == PhysicianQueueEvidence.ConsultStartEventName
            && e.ReffId == "DOC-Q1/No.15");
        tracker.ListEvent.Should().Contain(e =>
            e.EventName == PhysicianQueueEvidence.ConsultDoneEventName
            && e.ReffId == "DOC-Q1/No.15"
            && e.EventDate == TestTglJamProvider.Instance.Now);
    }

    [Fact]
    public async Task MulaiPeriksa_WhenAlreadyInService_ThenThrows()
    {
        var createdAt = new DateTime(2025, 5, 3, 7, 0, 0);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(createdAt), "BOOKING", "BK1", createdAt);
        var queue = new AntrianModel(
            "DOC-Q1", DateOnly.FromDateTime(createdAt), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag-doc", "Praktek", new ServicePointType("DR1", "Dokter"), [], _sequencer.Object);
        var entry = queue.AddEntry(15, tracker, "RG1", "REG", createdAt);
        entry.Serve(createdAt.AddMinutes(30));

        _queRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));

        var sut = new QueMulaiPeriksaHandler(
            _queRepo.Object, _trackerRepo.Object, TestTglJamProvider.Instance);
        var act = () => sut.Handle(new QueMulaiPeriksaCmd("DOC-Q1", 15), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MulaiPeriksa_WhenNotIdentified_ThenThrowsWithoutEvidence()
    {
        var createdAt = new DateTime(2025, 5, 3, 7, 0, 0);
        var queue = new AntrianModel(
            "DOC-Q1", DateOnly.FromDateTime(createdAt), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag-doc", "Praktek", new ServicePointType("DR1", "Dokter"), [], _sequencer.Object);
        queue.AddEntry(createdAt);

        _queRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));

        var sut = new QueMulaiPeriksaHandler(
            _queRepo.Object, _trackerRepo.Object, TestTglJamProvider.Instance);
        var act = () => sut.Handle(new QueMulaiPeriksaCmd("DOC-Q1", queue.ListEntry.Single().NoUrut),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not identified*");
        _trackerRepo.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }
}

/// <summary>
/// F-07: after Reg-style retag, physician entry must remain Waiting (ServedAt is not registration time).
/// </summary>
public class PhysicianQueueMilestoneSeparationTest
{
    private readonly Mock<ISequencer> _sequencer = new();

    public PhysicianQueueMilestoneSeparationTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(15);
    }

    [Fact]
    public void PhysicianEntry_AfterSetReffReg_StaysWaiting()
    {
        var createdAt = new DateTime(2025, 5, 2, 20, 0, 0);
        var regAt = new DateTime(2025, 5, 3, 8, 5, 0);
        var person = new PersonType("SITI", new DateOnly(1990, 5, 1));
        var tracker = PasienTrackerModel.Create(
            person, DateOnly.FromDateTime(regAt), "BOOKING", "BK1", createdAt);
        var queue = new AntrianModel(
            "DOC-Q1", DateOnly.FromDateTime(regAt), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag-doc", "Praktek", new ServicePointType("DR1", "Dokter"), [], _sequencer.Object);
        queue.AddEntry(15, tracker, "BK1", "BOK", createdAt);

        var entry = queue.ListEntry.Single(e => e.NoUrut == 15);
        entry.SetReff("RG001", "REG");

        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        entry.ReffDesc.Should().Be("REG");
        entry.ReffId.Should().Be("RG001");
        entry.ServedAt.Should().Be(new DateTime(3000, 1, 1));
    }
}
