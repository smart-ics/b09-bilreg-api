using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianEntryAssignPasienTest
{
    [Fact]
    public void AssignPasien_WhenAnonymous_ThenSetsTrackerAndVisitor()
    {
        var entry = AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "-", "-",
            new DateTime(2025, 8, 3, 6, 51, 0));
        var person = new PersonType("SINTA", new DateOnly(2008, 5, 5));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 8, 3), "BOOKING", "B1",
            new DateTime(2025, 8, 1, 9, 0, 0));

        entry.AssignPasien(tracker);

        entry.Tracker.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        entry.Visitor.PersonName.Should().Be("SINTA");
        entry.Visitor.TglLahir.Should().Be(new DateOnly(2008, 5, 5));
    }

    [Fact]
    public void AssignPasien_WhenAlreadyIdentified_ThenThrows()
    {
        var person = new PersonType("SINTA", new DateOnly(2008, 5, 5));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 8, 3), "BOOKING", "B1",
            new DateTime(2025, 8, 1, 9, 0, 0));
        var entry = AntrianEntryModel.Create(
            1, person, tracker, "-", "-",
            new DateTime(2025, 8, 3, 6, 51, 0));
        var other = PasienTrackerModel.Create(
            person, new DateOnly(2025, 8, 3), "BOOKING", "B2",
            new DateTime(2025, 8, 1, 9, 0, 0));

        var act = () => entry.AssignPasien(other);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignPasien_WhenSentinelTracker_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "-", "-");

        var act = () => entry.AssignPasien(PasienTrackerModel.Default);

        act.Should().Throw<ArgumentException>();
    }
}

public class QueueEvidenceReferenceTest
{
    [Fact]
    public void Create_WhenValid_ThenFormatsAntrianIdAndNoUrut()
    {
        var reff = QueueEvidenceReference.Create("01HXYZABCDEFGHJKMNPQRSTVWXY", 1);
        reff.Value.Should().Be("01HXYZABCDEFGHJKMNPQRSTVWXY/No.1");
    }

    [Fact]
    public void Create_WhenEmptyAntrianId_ThenThrows()
    {
        var act = () => QueueEvidenceReference.Create(" ", 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenNonPositiveNoUrut_ThenThrows()
    {
        var act = () => QueueEvidenceReference.Create("AN1", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class AntrianAnonymousAddEntryTest
{
    [Fact]
    public void AddEntry_WhenAnonymous_ThenReturnsWaitingEntryWithSentinelTracker()
    {
        var sequencer = new Mock<ISequencer>();
        sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(7);
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "AN001", new DateOnly(2025, 8, 3), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", [], sequencer.Object);

        var entry = queue.AddEntry(createdAt);

        entry.NoUrut.Should().Be(7);
        entry.Tracker.PasienTrackerId.Should().Be("-");
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        entry.CreatedAt.Should().Be(createdAt);
        queue.ListEntry.Should().ContainSingle(e => e.NoUrut == 7);
    }
}
