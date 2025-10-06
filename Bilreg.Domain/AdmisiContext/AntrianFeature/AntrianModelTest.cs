using Bilreg.Domain.AdmisiContext.BookingFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModelTest
{
    [Fact]
    public void T01_GivenValidDateAndDayJadwal_WhenCrete_ThenCreateSuccess()
    {
        var antrianDate = DateOnly.FromDateTime(new DateTime(2025, 10, 7));
        var jadwalPraktek = JadwalPraktekType.Default with { Hari = DayOfWeek.Tuesday };
        var actual = AntrianModel.Create(antrianDate,  jadwalPraktek);
        actual.Should().NotBeNull();
    }

    [Fact]
    public void T02_GivenInvalidDateAndDayJadwal_WhenCrete_ThenThrowEx()
    {
        var antrianDate = DateOnly.FromDateTime(new DateTime(2025, 10, 7));
        var jadwalPraktek = JadwalPraktekType.Default with { Hari = DayOfWeek.Monday };
        var actual = () => AntrianModel.Create(antrianDate, jadwalPraktek);
        actual.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void T03_GivenValidServicePoint_WhenCrete_ThenCreateSuccess()
    {
        var servicePoint = ServicePointType.Default;
        var actual = AntrianModel.Create(servicePoint);
        actual.Should().NotBeNull();
    }
    
    [Fact]
    public void T04_GivenClosedServicePoint_WhenCrete_ThenThrowEx()
    {
        var servicePoint = ServicePointType.Default with { Status = ServicePointStatusEnum.Closed };
        var actual = () => AntrianModel.Create(servicePoint);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T05_GivenValidPasienTracker_WhenAddEntry_ThenAddSuccess()
    {
        //  arrange
        var servicePoint = ServicePointType.Default with { Status = ServicePointStatusEnum.Opened };
        var antrian = AntrianModel.Create(servicePoint);
        var pasienTracker = PasienTrackerModel.Create(PersonType.Default);
        //  act
        antrian.AddEntry(pasienTracker);
        //  assert
        antrian.ListEntry.Should().HaveCount(1);
    }
}