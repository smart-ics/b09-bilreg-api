using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.BookingFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class BookingCreateFromHidokHandlerTest
{
    private readonly Mock<IJadwalPraktekRepo> _jadwalRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IPasienRepo> _pasienRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IAddAntrianEmrByBookingService> _addBookingSvc = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IQueueNumberCompatibilityAdapter> _queueAdapter = new();
    private readonly Mock<IClinicQueueRefreshPublisher> _clinicQueueRefreshPublisher = new();
    private readonly BookingCreateFromHidokHandler _sut;

    public BookingCreateFromHidokHandlerTest()
    {
        _sut = new BookingCreateFromHidokHandler(
            _jadwalRepo.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _bookingRepo.Object,
            _trackerRepo.Object,
            _pasienRepo.Object,
            _ppaRepo.Object,
            _addBookingSvc.Object,
            _featureResolver.Object,
            _queueAdapter.Object,
            TestTglJamProvider.Instance,
            _clinicQueueRefreshPublisher.Object);
    }

    [Fact]
    public async Task Handle_WhenValidBookingCreated_PublishesRefreshHintForPoliAndDokter()
    {
        // Arrange
        var tglBerobat = new DateOnly(2025, 10, 24);
        var jamMulai = new TimeOnly(8, 0);
        var dokter = PpaType.Default with { PpaId = "DR1", PpaName = "Dr. Budi" };
        var layanan = LayananType.Default with { LayananId = "LY1", LayananName = "Poli Umum" };
        var jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JP1",
            Hari = tglBerobat.DayOfWeek,
            JamMulai = jamMulai,
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter.ToReff(),
            Layanan = layanan.ToReff()
        };

        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<ContactFinder>()))
            .Returns(MayBe.From(dokter));

        _featureResolver.SetupGet(x => x.UseResolver).Returns(false);
        _jadwalRepo.Setup(x => x.ListData(It.IsAny<IPpaKey>())).Returns([jadwal]);

        var antrian = new AntrianModel(
            "AN1", tglBerobat, jamMulai, new TimeOnly(12, 0),
            AntrianModel.GenSequenceTag(tglBerobat, jadwal), "desc",
            new ServicePointType(dokter.PpaId.Replace(' ', '$'), "desc"),
            [], null!);
        _antrianRepo.Setup(x => x.ListData(tglBerobat)).Returns([]);
        _antrianFactory
            .Setup(x => x.Create(tglBerobat, It.IsAny<JadwalPraktekType>()))
            .Returns(antrian);

        var person = new PersonInfoType("ANI", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var fakeTracker = PasienTrackerModel.Create(
            new PersonType(person.PersonName, person.TglLahir),
            tglBerobat, "BOOKING", "BK1", new DateTime(2025, 10, 24, 8, 0, 0));
        var entry = AntrianEntryModel.Create(5, person, fakeTracker, "BK1", "BOK", new DateTime(2025, 10, 24, 8, 0, 0));

        _queueAdapter
            .Setup(x => x.AcceptExternalNumber(
                It.IsAny<AntrianModel>(),
                5,
                It.IsAny<PasienTrackerModel>(),
                It.IsAny<string>(),
                "BOK",
                It.IsAny<DateTime>()))
            .Returns(entry);

        var cmd = new BookingCreateFromHidokCommand(
            PasienId: "",
            PasienName: "ANI",
            TglLahir: "2000-01-02",
            Gender: "P",
            Alamat: "Jl Merdeka",
            NoTelp: "0812345",
            DokterEmail: "budi@hospital.com",
            TglBerobat: "2025-10-24",
            JamMulai: "08:00",
            NoAntrian: 5,
            AsuransiName: "BPJS",
            NoPeserta: "000123456",
            NoRujukan: "RUJ001",
            ReffId: "HIDOK-99",
            UserId: "user1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NoAntrian.Should().Be(5);
        _bookingRepo.Verify(x => x.SaveChanges(It.IsAny<BookingModel>()), Times.Once);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Once);
        _trackerRepo.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Once);

        _clinicQueueRefreshPublisher.Verify(
            x => x.PublishRefreshHint("LY1", "DR1", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
