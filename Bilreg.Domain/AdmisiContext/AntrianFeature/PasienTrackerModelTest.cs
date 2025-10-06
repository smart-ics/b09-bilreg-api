using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerModelTest
{
    [Fact]
    public void T01_GivenValidPerson_WhenCreate_ThenSuccess()
    {
        var person = new PersonType("A", new DateTime(1990, 10, 2),
            AlamatType.Default,
            ContactType.Default, IdentitasType.Default);
        var actual = PasienTrackerModel.Create(person);

        actual.Should().NotBeNull();
        actual.Visitor.VisitorName.Should().BeEquivalentTo(person.PersonName);
    }

    [Fact]
    public void T02_GivenValidReff_WhenAddEvent_ThenSuccess()
    {
        var tracker = new PasienTrackerModel("A", VisitorType.Default, ServicePointType.Default,
            ServicePointStatusEnum.Opened, new List<PasienTrackerEventModel>());
        string eventName = "B";
        string reff = "C";
        // act
        tracker.AddEvent(eventName, reff);
        // assert
        tracker.Events.Should().HaveCount(1);
    }
}
