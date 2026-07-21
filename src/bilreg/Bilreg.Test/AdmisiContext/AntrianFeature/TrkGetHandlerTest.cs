using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class TrkGetHandlerTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();

    [Fact]
    public async Task Get_WhenTrackerExists_ThenReturnsHeaderAndChronologicalEvents()
    {
        var createdAt = new DateTime(2025, 10, 10, 9, 0, 0);
        var laterAt = new DateTime(2025, 10, 24, 8, 30, 0);
        var person = new PersonType("ANI", new DateOnly(1990, 1, 1));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 10, 24), "BOOKING", "BK1", createdAt);
        tracker.AddEvent("REGISTER", "RG1", laterAt);

        _trackerRepo
            .Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == tracker.PasienTrackerId)))
            .Returns(MayBe.From(tracker));

        var sut = new TrkGetHandler(_trackerRepo.Object);
        var response = await sut.Handle(
            new TrkGetQuery(tracker.PasienTrackerId),
            CancellationToken.None);

        response.PasienTrackerId.Should().Be(tracker.PasienTrackerId);
        response.PersonName.Should().Be("ANI");
        response.TglLahir.Should().Be("1990-01-01");
        response.VisitDate.Should().Be("2025-10-24");
        response.StartPeriod.Should().Be("2025-10-10");
        response.LastPeriod.Should().Be("2025-10-24");

        var events = response.ListEvent.ToList();
        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("BOOKING");
        events[0].ReffId.Should().Be("BK1");
        events[1].EventName.Should().Be("REGISTER");
        events[1].ReffId.Should().Be("RG1");
    }

    [Fact]
    public async Task Get_WhenTrackerMissing_ThenThrows()
    {
        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe<PasienTrackerModel>.None);

        var sut = new TrkGetHandler(_trackerRepo.Object);
        var act = () => sut.Handle(new TrkGetQuery("MISSING"), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*MISSING*");
    }
}
