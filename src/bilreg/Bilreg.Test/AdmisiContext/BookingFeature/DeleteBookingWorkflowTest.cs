using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class DeleteBookingWorkflowTest
{
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IJadwalPraktekRepo> _jadwalRepo = new();
    private readonly Mock<IJadwalPraktekHarianRepo> _jadwalHarianRepo = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IQueueNumberCompatibilityAdapter> _queueAdapter = new();
    private readonly DeleteBookingWorkflow _sut;

    public DeleteBookingWorkflowTest()
    {
        _featureResolver.SetupGet(x => x.UseResolver).Returns(true);
        _sut = new DeleteBookingWorkflow(
            _bookingRepo.Object,
            _ppaRepo.Object,
            _antrianRepo.Object,
            _trackerRepo.Object,
            _antrianMapRepo.Object,
            _jadwalRepo.Object,
            _jadwalHarianRepo.Object,
            _featureResolver.Object,
            _queueAdapter.Object,
            TestTglJamProvider.Instance);
    }

    [Fact]
    public async Task Execute_WhenBookingDeleted_ThenRetainsTrackerAndAppendsBookingCancelled()
    {
        // Arrange
        var tgl = new DateOnly(2025, 10, 24);
        var jam = new TimeOnly(8, 0);
        var dokter = PpaType.Default with { PpaId = "D1", PpaName = "Dokter Satu" };
        var person = new PersonInfoType("Pasien A", new DateOnly(2000, 1, 2), "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var booking = new BookingModel(
            "BK1",
            new DateTime(2025, 10, 1, 9, 0, 0),
            person,
            "-",
            RegModel.Default.ToReff(),
            tgl,
            jam,
            LayananType.Default.ToReff(),
            dokter.ToReff(),
            1,
            AuditTrailType.Default,
            ExtAppReffType.Default,
            CoverageInfoType.Default,
            jadwalPraktekId: "JP1");

        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 1, 9, 0, 0));
        var stableId = tracker.PasienTrackerId;

        var entry = AntrianEntryModel.Create(1, tracker.Person, tracker, booking.BookingId, "BOK",
            new DateTime(2025, 10, 1, 9, 0, 0));
        var tag = AntrianModel.GenSequenceTag(tgl, jam, dokter);
        var antrian = new AntrianModel("AN1", tgl, jam, new TimeOnly(12, 0), tag, "desc",
            new ServicePointType(dokter.PpaId.Replace(' ', '$'), "desc"),
            [entry], null!);
        var header = new AntrianHeaderView("AN1", "desc", tgl, jam, tag);
        var jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JP1",
            Dokter = dokter.ToReff(),
            JamMulai = jam,
            Hari = tgl.DayOfWeek
        };

        _bookingRepo.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(booking));
        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokter));
        _antrianRepo.Setup(x => x.ListData(tgl)).Returns([header]);
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(antrian));
        _jadwalRepo.Setup(x => x.LoadEntity(It.IsAny<IJadwalPraktekKey>())).Returns(MayBe.From(jadwal));
        _antrianMapRepo.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
            .Returns(Enumerable.Empty<AntrianMapHdrView>());
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>())).Returns(MayBe.From(tracker));

        PasienTrackerModel? saved = null;
        _trackerRepo.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => saved = m);

        // Act
        await _sut.Execute(BookingModel.Key("BK1"));

        // Assert
        _trackerRepo.Verify(x => x.DeleteEntity(It.IsAny<IPasienTrackerKey>()), Times.Never);
        _bookingRepo.Verify(x => x.DeleteEntity(It.IsAny<IBookingKey>()), Times.Once);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Once);
        saved.Should().NotBeNull();
        saved!.PasienTrackerId.Should().Be(stableId);
        saved.ListEvent.Should().Contain(x =>
            x.EventName == "BOOKING_CANCELLED" && x.ReffId == "BK1");
    }
}
