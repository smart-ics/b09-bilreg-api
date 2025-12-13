using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PasienTrackerModelTest
{
    private static PersonInfoType CreatePersonFaker() 
        => new("John Doe", new DateOnly(1990, 5, 10), 
            "-",  AlamatType.Default, ContactType.Default, IdentitasType.Default);

    [Fact]
    public void UT1_GivenBooking_WhenCreateCalled_ThenAsExpected()
    {
        // Arrange
        var person = new PersonInfoType("A", new DateOnly(2000, 1, 2), "P", 
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var jadwal = JadwalPraktekType.Default with { Hari = DayOfWeek.Friday };
        var booking = BookingModel.CreateLocal(person, new DateOnly(2025, 10, 24), 
            jadwal, "-");

        // Act
        var tracker = PasienTrackerModel.Create(booking);

        // Assert
        tracker.Should().NotBeNull();
        tracker.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        tracker.Person.PersonName.Should().Be(person.PersonName);
        tracker.Person.TglLahir.Should().Be(person.TglLahir);
        tracker.ListEvent.Should().NotBeEmpty();
    }

    [Fact]
    public void UT2_GivenNoInput_WhenDefaultPropertyAccessed_ThenReturnObjectWithDefaultValues()
    {
        // Act
        var tracker = PasienTrackerModel.Default;

        // Assert
        tracker.PasienTrackerId.Should().Be("-");
        tracker.Person.Should().Be(PersonType.Default);
        tracker.ListEvent.Should().BeEmpty();
    }
}

