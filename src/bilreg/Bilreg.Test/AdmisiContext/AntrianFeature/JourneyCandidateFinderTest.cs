using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class JourneyCandidateFinderTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly JourneyCandidateFinder _sut;

    public JourneyCandidateFinderTest()
    {
        _sut = new JourneyCandidateFinder(_trackerRepo.Object);
    }

    [Fact]
    public void Find_WhenMultipleEydNameMatches_ThenReturnsAllWithEvidence()
    {
        var tglLahir = new DateOnly(2000, 1, 2);
        var visitDate = new DateOnly(2025, 10, 24);
        var person = new PersonType("SOEDJOYO", tglLahir);

        var tracker1 = CreateTracker("T1", person, visitDate, "BOOKING", "B1");
        var tracker2 = CreateTracker("T2", person, visitDate, "BOOKING", "B2");
        var otherName = CreateTracker("T3", new PersonType("BUDI", tglLahir), visitDate, "BOOKING", "B3");

        _trackerRepo
            .Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([
                ToView(tracker1),
                ToView(tracker2),
                ToView(otherName)
            ]);

        _trackerRepo.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "T1")))
            .Returns(MayBe.From(tracker1));
        _trackerRepo.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "T2")))
            .Returns(MayBe.From(tracker2));

        // EYD: SOEDJOYO → SUDJOYO (OE→U); query uses SUDJOYO
        var result = _sut.Find("SUDJOYO", tglLahir, visitDate);

        result.Should().HaveCount(2);
        result.Select(x => x.PasienTrackerId).Should().BeEquivalentTo("T1", "T2");
        result.Should().OnlyContain(x => x.Events.Any());
        result.First(x => x.PasienTrackerId == "T1").Events.First().ReffId.Should().Be("B1");
    }

    [Fact]
    public void Find_WhenNoNameMatch_ThenReturnsEmpty()
    {
        var tglLahir = new DateOnly(2000, 1, 2);
        var visitDate = new DateOnly(2025, 10, 24);
        var tracker = CreateTracker("T1", new PersonType("BUDI", tglLahir), visitDate, "BOOKING", "B1");

        _trackerRepo
            .Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([ToView(tracker)]);

        var result = _sut.Find("ANI", tglLahir, visitDate);

        result.Should().BeEmpty();
        _trackerRepo.Verify(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()), Times.Never);
    }

    private static PasienTrackerModel CreateTracker(
        string id, PersonType person, DateOnly visitDate, string eventName, string reffId)
    {
        var occurredAt = visitDate.ToDateTime(new TimeOnly(9, 0));
        var tracker = PasienTrackerModel.Create(person, visitDate, eventName, reffId, occurredAt);
        return new PasienTrackerModel(
            id, person, visitDate, tracker.StartPeriod, tracker.LastPeriod, tracker.ListEvent);
    }

    private static PasienTrackerView ToView(PasienTrackerModel model) =>
        new(model.PasienTrackerId, model.Person, model.VisitDate, model.StartPeriod, model.LastPeriod);
}
