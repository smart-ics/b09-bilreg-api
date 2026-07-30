using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianRepoAreEqualTest
{
    [Fact]
    public void ConditionalTransition_WhenDalAffectsOneRow_ThenReturnsTrue()
    {
        var sequencer = new Mock<ISequencer>();
        var antrianDal = new Mock<IAntrianDal>();
        var entryDal = new Mock<IAntrianEntryDal>();
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        var entry = queue.AddEntry(createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        var tracker = PasienTrackerModel.Create(
            new PersonType("SINTA", new DateOnly(2008, 5, 5)),
            DateOnly.FromDateTime(createdAt), "BOOKING", "B1", createdAt.AddDays(-1));
        entry.AssignPasien(tracker);
        entryDal.Setup(x => x.UpdateFromAnonymousInService(It.IsAny<AntrianEntryDto>()))
            .Returns(1);

        var sut = new AntrianRepo(antrianDal.Object, entryDal.Object, sequencer.Object);

        sut.TrySaveAnonymousInServiceTransition(queue, entry).Should().BeTrue();
        entryDal.Verify(x => x.UpdateFromAnonymousInService(
            It.Is<AntrianEntryDto>(d => d.PasienTrackerId == tracker.PasienTrackerId)), Times.Once);
    }

    [Fact]
    public void TrySaveWaitingToInService_WhenDalAffectsOneRow_ThenReturnsTrue()
    {
        var sequencer = new Mock<ISequencer>();
        var antrianDal = new Mock<IAntrianDal>();
        var entryDal = new Mock<IAntrianEntryDal>();
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        var entry = queue.AddEntry(createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        entryDal.Setup(x => x.UpdateWaitingToInService(It.IsAny<AntrianEntryDto>()))
            .Returns(1);

        var sut = new AntrianRepo(antrianDal.Object, entryDal.Object, sequencer.Object);

        sut.TrySaveWaitingToInServiceTransition(queue, entry).Should().BeTrue();
        entryDal.Verify(x => x.UpdateWaitingToInService(It.IsAny<AntrianEntryDto>()), Times.Once);
        entryDal.Verify(x => x.Delete(It.IsAny<IAntrianKey>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void TrySaveInServiceToDone_WhenDalAffectsZeroRows_ThenReturnsFalse()
    {
        var sequencer = new Mock<ISequencer>();
        var antrianDal = new Mock<IAntrianDal>();
        var entryDal = new Mock<IAntrianEntryDal>();
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        var entry = queue.AddEntry(createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        var tracker = PasienTrackerModel.Create(
            new PersonType("SINTA", new DateOnly(2008, 5, 5)),
            DateOnly.FromDateTime(createdAt), "BOOKING", "B1", createdAt.AddDays(-1));
        entry.AssignPasien(tracker);
        entry.Done(createdAt.AddMinutes(15));
        entryDal.Setup(x => x.UpdateInServiceToDone(It.IsAny<AntrianEntryDto>()))
            .Returns(0);

        var sut = new AntrianRepo(antrianDal.Object, entryDal.Object, sequencer.Object);

        sut.TrySaveInServiceToDoneTransition(queue, entry).Should().BeFalse();
        entryDal.Verify(x => x.Delete(It.IsAny<IAntrianKey>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SaveNewEntry_InsertsSingleEntryWithoutDeletingSiblings()
    {
        var sequencer = new Mock<ISequencer>();
        var antrianDal = new Mock<IAntrianDal>();
        var entryDal = new Mock<IAntrianEntryDal>();
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("ADM", "Loket Admisi"), [], sequencer.Object);
        var entry = queue.AddEntry(createdAt);
        antrianDal.Setup(x => x.GetData(It.IsAny<IAntrianKey>()))
            .Returns(AntrianDto.FromModel(queue));

        var sut = new AntrianRepo(antrianDal.Object, entryDal.Object, sequencer.Object);
        sut.SaveNewEntry(queue, entry);

        entryDal.Verify(x => x.Insert(It.Is<AntrianEntryDto>(d => d.NoUrut == entry.NoUrut)), Times.Once);
        entryDal.Verify(x => x.Delete(It.IsAny<IAntrianKey>(), It.IsAny<int>()), Times.Never);
        entryDal.Verify(x => x.ListData(It.IsAny<IAntrianKey>()), Times.Never);
        antrianDal.Verify(x => x.Update(It.IsAny<AntrianDto>()), Times.Once);
        antrianDal.Verify(x => x.GetData(It.IsAny<IAntrianKey>()), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenOnlyPasienTrackerIdChanges_ThenUpdatesEntry()
    {
        var sequencer = new Mock<ISequencer>();
        var antrianDal = new Mock<IAntrianDal>();
        var entryDal = new Mock<IAntrianEntryDal>();

        var antrianId = "AN001";
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var sentinelServed = new DateTime(3000, 1, 1);
        var entry = AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "-", "-", createdAt);
        var queue = new AntrianModel(
            antrianId, new DateOnly(2025, 8, 3), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("Loket", "Loket"), [entry], sequencer.Object);

        antrianDal
            .Setup(x => x.GetData(It.IsAny<IAntrianKey>()))
            .Returns(AntrianDto.FromModel(queue));
        entryDal
            .Setup(x => x.ListData(It.IsAny<IAntrianKey>()))
            .Returns([AntrianEntryDto.FromModel(antrianId, entry)]);

        var person = new PersonType("SINTA", new DateOnly(2008, 5, 5));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 8, 3), "BOOKING", "B1",
            new DateTime(2025, 8, 1, 9, 0, 0));
        // Name stays empty so only TrackerId differs from persisted DTO PersonName "".
        // AssignPasien also updates name; force TrackerId-only by reconstructing.
        var identified = new AntrianEntryModel(
            1, PersonType.Default, tracker, AntrianStatusEnum.Waiting,
            createdAt, sentinelServed, sentinelServed, "-", "-");
        var currentQueue = new AntrianModel(
            antrianId, new DateOnly(2025, 8, 3), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("Loket", "Loket"), [identified], sequencer.Object);

        AntrianEntryDto? updated = null;
        entryDal
            .Setup(x => x.Update(It.IsAny<AntrianEntryDto>()))
            .Callback<AntrianEntryDto>(d => updated = d);

        var sut = new AntrianRepo(antrianDal.Object, entryDal.Object, sequencer.Object);
        sut.SaveChanges(currentQueue);

        updated.Should().NotBeNull();
        updated!.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        entryDal.Verify(x => x.Update(It.IsAny<AntrianEntryDto>()), Times.Once);
    }
}
