using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerModelTest
{
    private static PersonInfoType CreatePersonFaker() 
        => new("John Doe", new DateOnly(1990, 5, 10), 
            "-",  AlamatType.Default, ContactType.Default, IdentitasType.Default);

    [Fact]
    public void UT1_GivenValidPerson_WhenCreateCalled_ThenReturnValidInstance()
    {
        // Arrange
        var person = CreatePersonFaker();

        // Act
        var tracker = PasienTrackerModel.Create(person);

        // Assert
        tracker.Should().NotBeNull();
        tracker.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        tracker.Person.Should().NotBeNull();
        tracker.Events.Should().BeEmpty();
    }

    [Fact]
    public void UT2_GivenNoInput_WhenDefaultPropertyAccessed_ThenReturnObjectWithDefaultValues()
    {
        // Act
        var tracker = PasienTrackerModel.Default;

        // Assert
        tracker.PasienTrackerId.Should().Be("-");
        tracker.Person.Should().Be(PersonType.Default);
        tracker.Events.Should().BeEmpty();
    }

    [Fact]
    public void UT3_GivenValidEventData_WhenAddEventCalled_ThenEventIsAddedToList()
    {
        // Arrange
        var tracker = PasienTrackerModel.Create(CreatePersonFaker());
        var beforeCount = tracker.Events.Count();

        // Act
        tracker.AddEvent("Registered", "REF001");

        // Assert
        tracker.Events.Count().Should().Be(beforeCount + 1);
        tracker.Events.Last().EventName.Should().Be("Registered");
        tracker.Events.Last().ReffId.Should().Be("REF001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UT4_GivenInvalidEventName_WhenAddEventCalled_ThenThrowArgumentException(string invalidName)
    {
        // Arrange
        var tracker = PasienTrackerModel.Create(CreatePersonFaker());

        // Act
        var act = () => tracker.AddEvent(invalidName, "REF001");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UT5_GivenInvalidReffId_WhenAddEventCalled_ThenThrowArgumentException(string invalidReffId)
    {
        // Arrange
        var tracker = PasienTrackerModel.Create(CreatePersonFaker());

        // Act
        var act = () => tracker.AddEvent("Registered", invalidReffId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

