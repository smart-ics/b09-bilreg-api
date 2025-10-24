using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModelTest
{
    [Fact]
    public void T01_GivenNotServe_WhenDone_ThrowException()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default);
        var actual = () => entry.Done();
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T02_GivenServed_WhenDone_ThenSuccess()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default);
        entry.Serve();
        var actual = () => entry.Done();
        actual.Should().NotThrow<ArgumentException>();
    }

    [Fact]
    public void T03_GivenValidVisitor_WhenAssign_ThrowSuccess()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default);
        var person = new PersonInfoType("A", DateOnly.Parse("2024-01-01"),
            "-",  AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var booking = BookingModel.Create(person, new DateOnly(2025, 10, 24), 
            JadwalPraktekType.Default);
        var tracker = PasienTrackerModel.Create(booking);
        entry.AssignPasien(tracker);
    
        entry.Visitor.Should().Be(tracker.Person);
    }
}
