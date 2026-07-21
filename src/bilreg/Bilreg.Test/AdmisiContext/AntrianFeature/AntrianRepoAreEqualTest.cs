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
            "tag", "Loket", [entry], sequencer.Object);

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
            "tag", "Loket", [identified], sequencer.Object);

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
