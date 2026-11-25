using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapWithBookingResolverTest
{
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly AntrianMapWithBookingResolver _sut;

    private readonly JadwalPraktekType _jadwal;
    private readonly DateOnly _tgl;

    public AntrianMapWithBookingResolverTest()
    {
        _tgl = new DateOnly(2025, 10, 24);
        _jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JADWAL01",
            Dokter = new PpaReff("DOK01", "dr. Budi"),
            Layanan = new LayananReff("LAY01", "Poli Umum"),
            Hari = _tgl.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 4,
            AntrianPattern = AntrianPatternType.Default
        };

        _featureResolver.Setup(x => x.UseResolver).Returns(false);
        _sut = new AntrianMapWithBookingResolver(_antrianMapRepo.Object, _featureResolver.Object);
    }

    [Fact]
    public void Resolve_WithoutOccupiedNumbers_AllocatesSlot1()
    {
        var antrianMap = AntrianMapModel.CreateFromJadwal(_jadwal, _tgl);
        _antrianMapRepo.Setup(x => x.Find(_jadwal, _tgl)).Returns(MayBe.From(antrianMap));
        var booking = BookingModel.CreateLocal(PersonInfoType.Default, _tgl, _jadwal, "U1", new DateTime(2025, 10, 1, 8, 0, 0));
        var pasien = PasienModel.Default;

        var result = _sut.Resolve(_jadwal, _tgl, booking, pasien);

        result.IsSuccess.Should().BeTrue();
        result.Value.Item2.NoUrut.Should().Be(1);
        result.Value.Item2.ReffId.Should().Be(booking.BookingId);
    }

    [Fact]
    public void Resolve_WhenSlot1FreeInMapButOccupiedInSession_SkipsSlot1AcrossUmumStrictTimeAutoAndOverflow()
    {
        var detils = new[]
        {
            new AntrianMapDetilModel(1, "", "", "", "UMUM", false),
            new AntrianMapDetilModel(2, "", "", "", "UMUM", false),
            new AntrianMapDetilModel(3, "", "", "", "08:00", false),
            new AntrianMapDetilModel(4, "", "", "", "AUTO", false)
        };
        var antrianMap = new AntrianMapModel(
            "MAP01", _jadwal.JadwalPraktekId, _jadwal.Dokter, _jadwal.Layanan,
            _tgl, _jadwal.JamMulai, _jadwal.JamMulai, _jadwal.AntrianPattern, 4, detils);

        _antrianMapRepo.Setup(x => x.Find(_jadwal, _tgl)).Returns(MayBe.From(antrianMap));
        var pasien = PasienModel.Default;
        var createdAt = new DateTime(2025, 10, 1, 8, 0, 0);

        // Slot 1 occupied in BILRG_AntrianEntry -> picks slot 2 (UMUM)
        var r1 = _sut.Resolve(_jadwal, _tgl, BookingModel.CreateLocal(PersonInfoType.Default, _tgl, _jadwal, "U1", createdAt), pasien, [1]);
        r1.Value.Item2.NoUrut.Should().Be(2);

        // Slot 3 (strict time) also occupied -> picks slot 4 (AUTO)
        var r2 = _sut.Resolve(_jadwal, _tgl, BookingModel.CreateLocal(PersonInfoType.Default, _tgl, _jadwal, "U1", createdAt), pasien, [1, 3]);
        r2.Value.Item2.NoUrut.Should().Be(4);

        // All 1..4 occupied/used and 5 occupied -> AddAuto picks 6
        var r3 = _sut.Resolve(_jadwal, _tgl, BookingModel.CreateLocal(PersonInfoType.Default, _tgl, _jadwal, "U1", createdAt), pasien, [1, 3, 5]);
        r3.Value.Item2.NoUrut.Should().Be(6);
    }
}
