using Bilreg.Application.AdmisiContext.AntrianFeature;
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

public class PasienTrackerStableIdentityTest
{
    [Fact]
    public void ForVisitChange_WhenExistingTracker_ThenReusesIdAndAppendsVisitChanged()
    {
        var booking = CreateBooking(new DateOnly(2025, 10, 24));
        var existing = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 10, 9, 0, 0));
        var stableId = existing.PasienTrackerId;
        var reg = CreateReg(new DateOnly(2025, 10, 24));

        var result = PasienTrackerStableIdentity.ForVisitChange(
            existing, reg, new DateTime(2025, 10, 24, 11, 0, 0));

        result.PasienTrackerId.Should().Be(stableId);
        result.ListEvent.Should().Contain(x =>
            x.EventName == "VISIT_CHANGED" && x.ReffId == reg.RegId);
        result.ListEvent.Should().Contain(x => x.EventName == "BOOKING");
    }

    [Fact]
    public void ForVisitChange_WhenNoExistingTracker_ThenCreatesAndAppendsVisitChanged()
    {
        var reg = CreateReg(new DateOnly(2025, 11, 1));

        var result = PasienTrackerStableIdentity.ForVisitChange(
            null, reg, new DateTime(2025, 11, 1, 10, 0, 0));

        result.PasienTrackerId.Should().NotBe("-");
        result.ListEvent.Select(x => x.EventName)
            .Should().Equal("REGISTER", "VISIT_CHANGED");
    }

    private static BookingModel CreateBooking(DateOnly tglBerobat)
    {
        var person = new PersonInfoType("A", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var jadwal = JadwalPraktekType.Default with { Hari = tglBerobat.DayOfWeek };
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
}
