using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class QueueNumberCompatibilityAdapterTest
{
    private readonly Mock<IAntrianMapWithBookingResolver> _bookingResolver = new();
    private readonly Mock<IAntrianMapWithRegResolver> _regResolver = new();
    private readonly QueueNumberCompatibilityAdapter _sut;

    public QueueNumberCompatibilityAdapterTest()
    {
        _sut = new QueueNumberCompatibilityAdapter(
            _bookingResolver.Object,
            _regResolver.Object,
            Options.Create(new QueueNumberCompatibilityOptions()));
    }

    [Fact]
    public void ReserveForBooking_ThenProjectIntoQueueSession_UsesSameNoUrut()
    {
        var tgl = new DateOnly(2025, 10, 24);
        var jadwal = JadwalPraktekType.Default with
        {
            Hari = tgl.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0)
        };
        var person = new PersonInfoType("ANI", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var booking = BookingModel.CreateLocal(
            person, tgl, jadwal, "U1", new DateTime(2025, 10, 1, 9, 0, 0));
        var pasien = PasienModel.Default;
        var mapHdr = AntrianMapModel.Default;
        var mapDetil = new AntrianMapDetilModel(7, "", "", "", "AUTO", false);
        _bookingResolver
            .Setup(x => x.Resolve(jadwal, tgl, booking, pasien))
            .Returns(Result<(AntrianMapModel, AntrianMapDetilModel)>.Success((mapHdr, mapDetil)));

        var reserved = _sut.ReserveForBooking(jadwal, tgl, booking, pasien).Value;
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 1, 9, 0, 0));
        var queue = new AntrianModel(
            "AN1", tgl, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag", "desc", new ServicePointType("SP", "desc"), [], null!);

        var entry = _sut.ProjectIntoQueueSession(
            queue, reserved, tracker, booking.BookingId, "BOK", new DateTime(2025, 10, 1, 9, 0, 0));

        reserved.NoUrut.Should().Be(7);
        entry.NoUrut.Should().Be(7);
        mapDetil.IsTerpakai.Should().BeFalse();
    }

    [Fact]
    public void AfterReserveForBooking_MapOccupiedAndQueueWaiting_IsValidNonEquivalentState()
    {
        var tgl = new DateOnly(2025, 10, 24);
        var jadwal = JadwalPraktekType.Default with
        {
            Hari = tgl.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0)
        };
        var person = new PersonInfoType("ANI", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var booking = BookingModel.CreateLocal(
            person, tgl, jadwal, "U1", new DateTime(2025, 10, 1, 9, 0, 0));
        var mapHdr = AntrianMapModel.Default;
        var mapDetil = new AntrianMapDetilModel(3, "ANI", "P1", booking.BookingId, "UMUM", true);
        var reserved = ReservedQueueNumber.FromMap(mapHdr, mapDetil);
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 1, 9, 0, 0));
        var queue = new AntrianModel(
            "AN1", tgl, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "tag", "desc", new ServicePointType("SP", "desc"), [], null!);
        var entry = _sut.ProjectIntoQueueSession(
            queue, reserved, tracker, booking.BookingId, "BOK", new DateTime(2025, 10, 1, 9, 0, 0));

        mapDetil.IsTerpakai.Should().BeTrue();
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
    }

    [Fact]
    public void Release_ClearsSlotForReuse()
    {
        var detil = new AntrianMapDetilModel(5, "ANI", "P1", "BK1", "UMUM", true);
        var map = new AntrianMapModel(
            "M1", "J1", new PpaReff("D1", "Dokter"), new LayananReff("L1", "Layanan"),
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(8, 0), new TimeOnly(12, 0),
            AntrianPatternType.Default, 10, [detil]);

        _sut.Release(map, 5);

        detil.IsFreeSlot().Should().BeTrue();
        detil.Flag.Should().Be("AUTO");
    }

    [Fact]
    public void ReserveForRegistration_WhenQueueSessionAuthority_Throws()
    {
        var sut = new QueueNumberCompatibilityAdapter(
            _bookingResolver.Object,
            _regResolver.Object,
            Options.Create(new QueueNumberCompatibilityOptions
            {
                Authority = QueueNumberAuthority.QueueSession
            }));

        var act = () => sut.ReserveForRegistration(
            JadwalPraktekType.Default,
            DateOnly.FromDateTime(DateTime.Today),
            RegModel.Default);

        act.Should().Throw<NotSupportedException>();
    }
}
