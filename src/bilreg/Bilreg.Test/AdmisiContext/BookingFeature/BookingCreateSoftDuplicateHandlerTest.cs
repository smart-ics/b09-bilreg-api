using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.BookingFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class BookingCreateSoftDuplicateHandlerTest
{
    private readonly Mock<IJadwalPraktekRepo> _jadwalRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IPasienRepo> _pasienRepo = new();
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IAddAntrianEmrByBookingService> _emr = new();
    private readonly Mock<IAntrianMapWithBookingResolver> _mapResolver = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IJourneyCandidateFinder> _candidateFinder = new();
    private readonly BookingCreateHandler _sut;

    public BookingCreateSoftDuplicateHandlerTest()
    {
        _sut = new BookingCreateHandler(
            _jadwalRepo.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _bookingRepo.Object,
            _trackerRepo.Object,
            _pasienRepo.Object,
            _antrianMapRepo.Object,
            _emr.Object,
            _mapResolver.Object,
            _featureResolver.Object,
            _candidateFinder.Object,
            TestTglJamProvider.Instance);
    }

    [Fact]
    public async Task Handle_WhenCandidatesExistAndNotForced_ThenReturnsSoftDuplicateWithoutCreating()
    {
        var candidates = new List<TrkJourneyCandidateDto>
        {
            new("T1", "ANI", "2000-01-02", "2025-10-24", "2025-10-10", "2025-10-24",
            [
                new TrkJourneyCandidateEventDto(1, "BOOKING", "2025-10-10 09:00:00", "B1")
            ])
        };
        _candidateFinder
            .Setup(x => x.Find("ANI", new DateOnly(2000, 1, 2), new DateOnly(2025, 10, 24)))
            .Returns(candidates);

        var cmd = BaseCmd(isForce: false, selectedTrackerId: "");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsDuplicated.Should().BeTrue();
        result.IsCreated.Should().BeFalse();
        result.BookingId.Should().Be("-");
        result.NoAntrian.Should().Be(0);
        result.Candidates.Should().HaveCount(1);
        _bookingRepo.Verify(x => x.SaveChanges(It.IsAny<BookingModel>()), Times.Never);
        _emr.Verify(x => x.Execute(It.IsAny<AddAntrianEmrByBookingCmd>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenForcedDespiteCandidates_ThenCreatesNewTracker()
    {
        SetupCreatePath();
        _candidateFinder
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns([
                new TrkJourneyCandidateDto("T1", "ANI", "2000-01-02", "2025-10-24",
                    "2025-10-10", "2025-10-24", [])
            ]);

        PasienTrackerModel? savedTracker = null;
        _trackerRepo
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var result = await _sut.Handle(BaseCmd(isForce: true), CancellationToken.None);

        result.IsCreated.Should().BeTrue();
        result.IsDuplicated.Should().BeFalse();
        result.BookingId.Should().NotBe("-");
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().Contain(e => e.EventName == "BOOKING");
        _candidateFinder.Verify(
            x => x.Find(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSelectedTrackerId_ThenReusesTrackerAndAppendsBooking()
    {
        SetupCreatePath();
        var existing = PasienTrackerModel.Create(
            new PersonType("ANI", new DateOnly(2000, 1, 2)),
            new DateOnly(2025, 10, 24),
            "BOOKING", "B-OLD",
            new DateTime(2025, 10, 10, 9, 0, 0));
        var stableId = existing.PasienTrackerId;

        _trackerRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(existing));

        PasienTrackerModel? savedTracker = null;
        _trackerRepo
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var result = await _sut.Handle(
            BaseCmd(isForce: false, selectedTrackerId: stableId),
            CancellationToken.None);

        result.IsCreated.Should().BeTrue();
        result.PasienTrackerId.Should().Be(stableId);
        savedTracker!.PasienTrackerId.Should().Be(stableId);
        savedTracker.ListEvent.Count(e => e.EventName == "BOOKING").Should().Be(2);
        _candidateFinder.Verify(
            x => x.Find(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()),
            Times.Never);
    }

    private void SetupCreatePath()
    {
        var tglBerobat = new DateOnly(2025, 10, 24);
        var jamMulai = new TimeOnly(8, 0);
        var dokter = new PpaReff("DR1", "Dokter");
        var layanan = new LayananReff("LY1", "Layanan");
        var jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JP1",
            Hari = tglBerobat.DayOfWeek,
            JamMulai = jamMulai,
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter,
            Layanan = layanan
        };

        _featureResolver.SetupGet(x => x.UseResolver).Returns(false);
        _jadwalRepo.Setup(x => x.ListData(It.IsAny<IPpaKey>())).Returns([jadwal]);

        var antrian = new AntrianModel(
            "AN1", tglBerobat, jamMulai, new TimeOnly(12, 0),
            AntrianModel.GenSequenceTag(tglBerobat, jadwal), "desc", [], null!);
        _antrianRepo.Setup(x => x.ListData(tglBerobat)).Returns([]);
        _antrianFactory
            .Setup(x => x.Create(tglBerobat, It.IsAny<JadwalPraktekType>()))
            .Returns(antrian);

        var mapHdr = AntrianMapModel.Default;
        var mapDetil = new AntrianMapDetilModel(7, "", "", "", "AUTO", false);
        _mapResolver
            .Setup(x => x.Resolve(
                It.IsAny<JadwalPraktekType>(),
                tglBerobat,
                It.IsAny<BookingModel>(),
                It.IsAny<PasienModel>()))
            .Returns(Result<(AntrianMapModel, AntrianMapDetilModel)>.Success((mapHdr, mapDetil)));
    }

    private static BookingCreateCmd BaseCmd(bool isForce, string selectedTrackerId = "") =>
        new(
            PasienId: "",
            PasienName: "ANI",
            TglLahir: "2000-01-02",
            Gender: "P",
            Alamat: "Jl A",
            NoTelp: "081",
            DokterId: "DR1",
            TglBerobat: "2025-10-24",
            JamMulai: "08:00",
            UserId: "user1",
            IsForceDuplicatedTracker: isForce,
            SelectedTrackerId: selectedTrackerId);
}
