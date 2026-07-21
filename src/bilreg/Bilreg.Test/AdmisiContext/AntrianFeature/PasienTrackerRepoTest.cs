using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PasienTrackerRepoTest
{
    private readonly Mock<IPasienTrackerDal> _headerDal = new();
    private readonly Mock<IPasienTrackerEventDal> _eventDal = new();
    private readonly PasienTrackerRepo _sut;

    public PasienTrackerRepoTest()
    {
        _sut = new PasienTrackerRepo(_headerDal.Object, _eventDal.Object);
    }

    [Fact]
    public void SaveChanges_WhenNewTracker_ThenInsertsHeaderAndEvent()
    {
        var person = new PersonType("Andi", new DateOnly(1990, 1, 1));
        var occurredAt = new DateTime(2025, 1, 2, 9, 0, 0);
        var model = new PasienTrackerModel(
            "TRK001", person, new DateOnly(2025, 1, 5),
            DateOnly.MinValue, new DateOnly(2025, 1, 5),
            []);
        model.AddEvent("BOOKING", "BOK001", occurredAt);

        _headerDal.Setup(x => x.GetData(It.IsAny<IPasienTrackerKey>()))
            .Returns(() => null!);
        _eventDal.Setup(x => x.ListData(It.IsAny<IPasienTrackerKey>())).Returns([]);

        _sut.SaveChanges(model);

        _headerDal.Verify(x => x.Insert(It.Is<PasienTrackerDto>(d => d.PasienTrackerId == "TRK001")), Times.Once);
        _headerDal.Verify(x => x.Update(It.IsAny<PasienTrackerDto>()), Times.Never);
        _eventDal.Verify(x => x.Insert(It.Is<PasienTrackerEventDto>(e =>
            e.PasienTrackerId == "TRK001" && e.NoUrut == 1 && e.EventName == "BOOKING")), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenAppendingEvent_ThenInsertsOnlyNewEvent()
    {
        var person = new PersonType("Andi", new DateOnly(1990, 1, 1));
        var firstAt = new DateTime(2025, 1, 2, 9, 0, 0);
        var secondAt = new DateTime(2025, 1, 2, 9, 0, 0);
        var model = new PasienTrackerModel(
            "TRK001", person, new DateOnly(2025, 1, 5),
            new DateOnly(2025, 1, 2), new DateOnly(2025, 1, 5),
            [new PasienTrackerEventType(1, "BOOKING", firstAt, "BOK001")]);
        model.AddEvent("CANCEL", "BOK001", secondAt);

        var headerDto = PasienTrackerDto.FromModel(model);
        var existingEvent = new PasienTrackerEventDto("TRK001", 1, "BOOKING", firstAt, "BOK001");

        _headerDal.Setup(x => x.GetData(It.IsAny<IPasienTrackerKey>())).Returns(headerDto);
        _eventDal.Setup(x => x.ListData(It.IsAny<IPasienTrackerKey>())).Returns([existingEvent]);

        _sut.SaveChanges(model);

        _headerDal.Verify(x => x.Update(It.IsAny<PasienTrackerDto>()), Times.Once);
        _headerDal.Verify(x => x.Insert(It.IsAny<PasienTrackerDto>()), Times.Never);
        _eventDal.Verify(x => x.Insert(It.Is<PasienTrackerEventDto>(e =>
            e.NoUrut == 2 && e.EventName == "CANCEL")), Times.Once);
        _eventDal.Verify(x => x.Insert(It.Is<PasienTrackerEventDto>(e => e.NoUrut == 1)), Times.Never);
    }

    [Fact]
    public void LoadEntity_ReturnsEventsInPersistedOrder()
    {
        var occurredAt = new DateTime(2025, 1, 2, 10, 0, 0);
        var headerDto = new PasienTrackerDto(
            "TRK001", "Andi",
            new DateTime(1990, 1, 1),
            new DateTime(2025, 1, 5),
            new DateTime(2025, 1, 2),
            new DateTime(2025, 1, 5));
        var events = new List<PasienTrackerEventDto>
        {
            new("TRK001", 1, "BOOKING", occurredAt, "BOK001"),
            new("TRK001", 2, "CANCEL", occurredAt, "BOK001"),
        };

        _headerDal.Setup(x => x.GetData(It.IsAny<IPasienTrackerKey>())).Returns(headerDto);
        _eventDal.Setup(x => x.ListData(It.IsAny<IPasienTrackerKey>())).Returns(events);

        var loaded = _sut.LoadEntity(PasienTrackerModel.Key("TRK001"));

        loaded.HasValue.Should().BeTrue();
        loaded.Match(
            onSome: model =>
            {
                model.ListEvent.Select(x => x.NoUrut).Should().Equal(1, 2);
                model.ListEvent.Select(x => x.EventName).Should().Equal("BOOKING", "CANCEL");
            },
            onNone: () => Assert.Fail("Expected tracker"));
    }

    [Fact]
    public void DeleteEntity_ThrowsNotSupported()
    {
        var act = () => _sut.DeleteEntity(PasienTrackerModel.Key("TRK001"));

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*append-only*");
        _headerDal.Verify(x => x.Delete(It.IsAny<IPasienTrackerKey>()), Times.Never);
    }
}
