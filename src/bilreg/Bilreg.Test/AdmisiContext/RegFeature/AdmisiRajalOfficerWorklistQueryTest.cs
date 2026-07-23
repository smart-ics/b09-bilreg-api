using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class AdmisiRajalOfficerWorklistQueryTest
{
    [Fact]
    public async Task AnonymousQueueRow_ReturnsQueueOnlyWithNullEnrichment()
    {
        var queue = SampleQueue(pasienTrackerId: null);
        var projection = new Mock<IAdmissionQueueOperationalProjection>(MockBehavior.Strict);
        projection.Setup(x => x.ListWorklist(It.IsAny<AdmissionQueueWorklistFilter>()))
            .Returns([queue]);
        var trackers = new Mock<IPasienTrackerRepo>(MockBehavior.Strict);
        var bookings = new Mock<IBookingRepo>(MockBehavior.Strict);
        var regs = new Mock<IRegRepo>(MockBehavior.Strict);
        var assistance = new Mock<IBookingAssistanceRepo>(MockBehavior.Strict);
        assistance.Setup(x => x.FindActiveByEntry(queue.AntrianId, queue.NoUrut))
            .Returns((BookingAssistanceActive?)null);

        var sut = new AdmisiRajalOfficerWorklistHandler(
            projection.Object, trackers.Object, bookings.Object, regs.Object, assistance.Object);

        var result = await sut.Handle(new("2026-07-23"), default);

        result.Should().HaveCount(1);
        result[0].Queue.Should().BeEquivalentTo(queue);
        result[0].Identity.Should().BeNull();
        result[0].Booking.Should().BeNull();
        result[0].Registration.Should().BeNull();
        trackers.VerifyNoOtherCalls();
        bookings.VerifyNoOtherCalls();
        regs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TrackerWithBookingAndRegisterEvents_ComposesIdentityBookingAndRegistration()
    {
        var queue = SampleQueue(pasienTrackerId: "TRK1");
        var projection = new Mock<IAdmissionQueueOperationalProjection>();
        projection.Setup(x => x.ListWorklist(It.IsAny<AdmissionQueueWorklistFilter>()))
            .Returns([queue]);

        var tracker = PasienTrackerModel.Create(
            new PersonType("Ani", new DateOnly(1990, 1, 2)),
            new DateOnly(2026, 7, 23),
            "BOOKING", "B1", new DateTime(2026, 7, 23, 8, 0, 0));
        tracker.AddEvent("REGISTER", "RG1", new DateTime(2026, 7, 23, 9, 0, 0));

        var trackers = new Mock<IPasienTrackerRepo>();
        trackers.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));

        var booking = BuildBooking("B1", "Ani", new DateOnly(1990, 1, 2), "P1");
        var bookings = new Mock<IBookingRepo>();
        bookings.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(booking));

        var reg = BuildReg("RG1", "P1", "Ani");
        var regs = new Mock<IRegRepo>();
        regs.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));

        var assistance = new Mock<IBookingAssistanceRepo>();
        assistance.Setup(x => x.FindActiveByEntry(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((BookingAssistanceActive?)null);

        var sut = new AdmisiRajalOfficerWorklistHandler(
            projection.Object, trackers.Object, bookings.Object, regs.Object, assistance.Object);

        var result = await sut.Handle(new("2026-07-23", ServicePointId: "ADM"), default);

        result.Should().HaveCount(1);
        result[0].Queue.AntrianId.Should().Be(queue.AntrianId);
        result[0].Identity!.PersonName.Should().Be("Ani");
        result[0].Identity.TglLahir.Should().Be("1990-01-02");
        result[0].Booking!.BookingId.Should().Be("B1");
        result[0].Registration!.RegId.Should().Be("RG1");
        result[0].Registration.PasienId.Should().Be("P1");
    }

    [Fact]
    public async Task AssistanceEntryWithoutTracker_ResolvesBookingEnrichment()
    {
        var queue = SampleQueue(pasienTrackerId: "-");
        var projection = new Mock<IAdmissionQueueOperationalProjection>();
        projection.Setup(x => x.ListWorklist(It.IsAny<AdmissionQueueWorklistFilter>()))
            .Returns([queue]);

        var assistance = new Mock<IBookingAssistanceRepo>();
        assistance.Setup(x => x.FindActiveByEntry(queue.AntrianId, queue.NoUrut))
            .Returns(new BookingAssistanceActive("B9", queue.AntrianId, queue.NoUrut, "A0001"));

        var booking = BuildBooking("B9", "Budi", new DateOnly(1985, 5, 5), "-");
        var bookings = new Mock<IBookingRepo>();
        bookings.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(booking));

        var trackers = new Mock<IPasienTrackerRepo>(MockBehavior.Strict);
        var regs = new Mock<IRegRepo>(MockBehavior.Strict);

        var sut = new AdmisiRajalOfficerWorklistHandler(
            projection.Object, trackers.Object, bookings.Object, regs.Object, assistance.Object);

        var result = await sut.Handle(new("2026-07-23"), default);

        result[0].Booking!.BookingId.Should().Be("B9");
        result[0].Identity!.PersonName.Should().Be("Budi");
        result[0].Registration.Should().BeNull();
        trackers.VerifyNoOtherCalls();
        regs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Composition_DoesNotCallQueueWritePorts()
    {
        var projection = new Mock<IAdmissionQueueOperationalProjection>();
        projection.Setup(x => x.ListWorklist(It.IsAny<AdmissionQueueWorklistFilter>()))
            .Returns([]);
        var queueWrites = new Mock<IAntrianRepo>(MockBehavior.Strict);
        var operations = new Mock<IAdmissionQueueOperationRepo>(MockBehavior.Strict);

        var sut = new AdmisiRajalOfficerWorklistHandler(
            projection.Object,
            Mock.Of<IPasienTrackerRepo>(),
            Mock.Of<IBookingRepo>(),
            Mock.Of<IRegRepo>(),
            Mock.Of<IBookingAssistanceRepo>());

        await sut.Handle(new("2026-07-23"), default);

        queueWrites.VerifyNoOtherCalls();
        operations.VerifyNoOtherCalls();
        projection.Verify(x => x.ListWorklist(It.IsAny<AdmissionQueueWorklistFilter>()), Times.Once);
    }

    private static AdmissionQueueWorklistItem SampleQueue(string? pasienTrackerId) => new(
        "Q1", 1, "A0001", "ADM", "Admission", 0, false,
        AdmissionQueueCreationReason.Normal, 0, null, null,
        new DateTime(2026, 7, 23, 8, 0, 0), null, null, pasienTrackerId);

    private static BookingModel BuildBooking(string id, string name, DateOnly dob, string pasienId)
    {
        var person = new PersonInfoType(name, dob, "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        return new BookingModel(
            id, new DateTime(2026, 7, 22), person, pasienId, RegModel.Default.ToReff(),
            new DateOnly(2026, 7, 23), new TimeOnly(8, 0),
            new LayananReff("LY1", "Poli"), new PpaReff("DR1", "Dokter"), 1,
            AuditTrailType.Default, ExtAppReffType.Default, CoverageInfoType.Default);
    }

    private static RegModel BuildReg(string regId, string pasienId, string name) =>
        new(regId, new DateOnly(2026, 7, 23),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff(pasienId, name, new DateOnly(1990, 1, 2), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DR1", "Dokter"),
            new LayananReff("LY1", "Poli"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default, []);
}
