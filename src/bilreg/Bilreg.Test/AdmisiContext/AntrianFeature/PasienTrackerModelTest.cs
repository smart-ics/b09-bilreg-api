using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PasienTrackerModelTest
{
    private static BookingModel CreateBooking(DateOnly tglBerobat)
    {
        var person = new PersonInfoType("A", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var jadwal = JadwalPraktekType.Default with { Hari = DayOfWeek.Friday };
        return BookingModel.CreateLocal(person, tglBerobat, jadwal, "-");
    }

    private static RegModel CreateReg(DateOnly regDate)
    {
        return new RegModel(
            "RG00000001",
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public void UT1_GivenBooking_WhenCreateCalled_ThenAsExpected()
    {
        // Arrange
        var booking = CreateBooking(new DateOnly(2025, 10, 24));

        // Act
        var tracker = PasienTrackerModel.Create(booking);

        // Assert
        tracker.Should().NotBeNull();
        tracker.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        tracker.Person.PersonName.Should().Be(booking.Person.PersonName);
        tracker.Person.TglLahir.Should().Be(booking.Person.TglLahir);
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
        tracker.VisitDate.Should().Be(new DateOnly(3000, 1, 1));
        tracker.StartPeriod.Should().Be(new DateOnly(3000, 1, 1));
        tracker.LastPeriod.Should().Be(new DateOnly(3000, 1, 1));
        tracker.ListEvent.Should().BeEmpty();
    }

    [Fact]
    public void UT3_GivenBookingWithEarlierOccurredAt_WhenCreate_ThenStartPeriodIsOccurredAtAndLastPeriodIsVisitDate()
    {
        // Arrange
        var visitDate = new DateOnly(2025, 10, 24);
        var booking = CreateBooking(visitDate);
        var occurredAt = new DateTime(2025, 10, 10, 9, 0, 0);

        // Act
        var tracker = PasienTrackerModel.Create(booking, occurredAt);

        // Assert
        tracker.VisitDate.Should().Be(visitDate);
        tracker.StartPeriod.Should().Be(new DateOnly(2025, 10, 10));
        tracker.LastPeriod.Should().Be(visitDate);
    }

    [Fact]
    public void UT4_GivenBookingOnSameDay_WhenCreate_ThenStartAndLastPeriodEqualVisitDate()
    {
        // Arrange
        var visitDate = new DateOnly(2025, 10, 24);
        var booking = CreateBooking(visitDate);
        var occurredAt = new DateTime(2025, 10, 24, 8, 30, 0);

        // Act
        var tracker = PasienTrackerModel.Create(booking, occurredAt);

        // Assert
        tracker.StartPeriod.Should().Be(visitDate);
        tracker.LastPeriod.Should().Be(visitDate);
    }

    [Fact]
    public void UT5_GivenReg_WhenCreate_ThenStartPeriodEqualsLastPeriodEqualsOccurredAtDate()
    {
        // Arrange
        var regDate = new DateOnly(2025, 11, 1);
        var reg = CreateReg(regDate);
        var occurredAt = new DateTime(2025, 11, 1, 10, 0, 0);

        // Act
        var tracker = PasienTrackerModel.Create(reg, occurredAt);

        // Assert
        tracker.VisitDate.Should().Be(regDate);
        tracker.StartPeriod.Should().Be(new DateOnly(2025, 11, 1));
        tracker.LastPeriod.Should().Be(new DateOnly(2025, 11, 1));
    }

    [Fact]
    public void UT6_GivenLaterEvidence_WhenAddEvent_ThenLastPeriodExtendsAndStartPeriodUnchanged()
    {
        // Arrange
        var visitDate = new DateOnly(2025, 10, 24);
        var booking = CreateBooking(visitDate);
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 10, 9, 0, 0));
        var priorStart = tracker.StartPeriod;

        // Act
        tracker.AddEvent("REGISTER", "RG1", new DateTime(2025, 10, 30, 11, 0, 0));

        // Assert
        tracker.StartPeriod.Should().Be(priorStart);
        tracker.LastPeriod.Should().Be(new DateOnly(2025, 10, 30));
    }

    [Fact]
    public void UT7_GivenEarlierEvidenceDate_WhenAddEvent_ThenLastPeriodDoesNotShrink()
    {
        // Arrange
        var visitDate = new DateOnly(2025, 10, 24);
        var booking = CreateBooking(visitDate);
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 10, 9, 0, 0));
        var priorLast = tracker.LastPeriod;

        // Act
        tracker.AddEvent("CHECKIN", "Q1", new DateTime(2025, 10, 12, 7, 0, 0));

        // Assert
        tracker.LastPeriod.Should().Be(priorLast);
        tracker.StartPeriod.Should().Be(new DateOnly(2025, 10, 10));
    }

    [Fact]
    public void UT8_GivenCancelAndRescheduleEvidence_WhenAddEvent_ThenTrackerIdRemainsStable()
    {
        // Arrange
        var booking = CreateBooking(new DateOnly(2025, 10, 24));
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 10, 9, 0, 0));
        var stableId = tracker.PasienTrackerId;

        // Act
        tracker.AddEvent("VISIT_CHANGED", "RG00000001", new DateTime(2025, 10, 24, 11, 0, 0));
        tracker.AddEvent("REGISTER_CANCELLED", "RG00000001", new DateTime(2025, 10, 24, 12, 0, 0));
        tracker.AddEvent("BOOKING_CANCELLED", booking.BookingId, new DateTime(2025, 10, 24, 13, 0, 0));

        // Assert
        tracker.PasienTrackerId.Should().Be(stableId);
        tracker.ListEvent.Select(x => x.EventName).Should().Contain(new[]
        {
            "BOOKING",
            "VISIT_CHANGED",
            "REGISTER_CANCELLED",
            "BOOKING_CANCELLED"
        });
    }

    [Fact]
    public void UT9_GivenPersonAndEvidence_WhenCreate_ThenEstablishesTrackerWithFirstEvent()
    {
        var person = new PersonType("ANI", new DateOnly(2000, 1, 2));
        var visitDate = new DateOnly(2025, 10, 24);
        var occurredAt = new DateTime(2025, 10, 24, 8, 0, 0);

        var tracker = PasienTrackerModel.Create(person, visitDate, "CHECKIN", "Q-1", occurredAt);

        tracker.PasienTrackerId.Should().NotBeNullOrWhiteSpace();
        tracker.Person.Should().Be(person);
        tracker.VisitDate.Should().Be(visitDate);
        tracker.StartPeriod.Should().Be(visitDate);
        tracker.LastPeriod.Should().Be(visitDate);
        tracker.ListEvent.Should().ContainSingle(e => e.EventName == "CHECKIN" && e.ReffId == "Q-1");
    }

    [Fact]
    public void UT10_GivenRegistrationAndAdmissionMilestones_WhenCreateWithAdmissionEvidence_ThenPreservesChronologicalEvidence()
    {
        var reg = CreateReg(new DateOnly(2025, 5, 3));
        var checkInAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var regStartAt = new DateTime(2025, 5, 3, 8, 10, 0);
        var registeredAt = new DateTime(2025, 5, 3, 8, 25, 0);

        var tracker = PasienTrackerModel.CreateFromRegistrationWithAdmissionEvidence(
            reg, "ADM-Q1/No.1", checkInAt, regStartAt, registeredAt);

        tracker.ListEvent.Select(x => x.EventName)
            .Should().ContainInOrder("Check In", "Reg-Start", "REGISTER");
        tracker.ListEvent.Select(x => x.EventDate)
            .Should().ContainInOrder(checkInAt, regStartAt, registeredAt);
        tracker.StartPeriod.Should().Be(DateOnly.FromDateTime(checkInAt));
        tracker.LastPeriod.Should().Be(DateOnly.FromDateTime(registeredAt));
    }
}
