using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext;

public class PasienTrackerInpatientCoordinatorTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly PasienTrackerInpatientCoordinator _sut;
    private readonly DateTime _defaultTime = new(2026, 10, 17, 10, 30, 0);

    public PasienTrackerInpatientCoordinatorTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_defaultTime);
        _sut = new PasienTrackerInpatientCoordinator(_trackerRepoMock.Object, _tglJamProviderMock.Object);
    }

    [Fact]
    public void RecordReservationCreated_WithParameters_CreatesTrackerWithReservasiEventAndSaves()
    {
        // Arrange
        var personName = "Budi Rahardjo";
        var tglLahir = new DateOnly(1985, 5, 20);
        var visitDate = new DateOnly(2026, 10, 20);
        var reservationId = "RSV-20261020-001";
        var occurredAt = new DateTime(2026, 10, 17, 8, 0, 0);

        PasienTrackerModel? savedTracker = null;
        _trackerRepoMock.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => savedTracker = m);

        // Act
        var result = _sut.RecordReservationCreated(personName, tglLahir, visitDate, reservationId, occurredAt);

        // Assert
        result.Should().NotBeNull();
        result.Person.PersonName.Should().Be(personName);
        result.Person.TglLahir.Should().Be(tglLahir);
        result.VisitDate.Should().Be(visitDate);
        result.ListEvent.Should().HaveCount(1);

        var firstEvent = result.ListEvent.First();
        firstEvent.EventName.Should().Be(PasienTrackerInpatientEvents.Reservasi);
        firstEvent.ReffId.Should().Be(reservationId);
        firstEvent.EventDate.Should().Be(occurredAt);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
        savedTracker.Should().BeSameAs(result);
    }

    [Fact]
    public void RecordReservationCreated_DefaultOccurredAt_UsesTglJamProviderNow()
    {
        // Arrange
        var personName = "Siti Aminah";
        var tglLahir = new DateOnly(1992, 3, 15);
        var visitDate = new DateOnly(2026, 10, 25);
        var reservationId = "RSV-20261025-002";

        // Act
        var result = _sut.RecordReservationCreated(personName, tglLahir, visitDate, reservationId);

        // Assert
        result.ListEvent.First().EventDate.Should().Be(_defaultTime);
        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordReservationCreated_WithReservationModel_CreatesTrackerCorrectly()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Hartono", new DateOnly(1975, 12, 1), "L");
        var plannedDate = new DateTime(2026, 11, 1, 9, 0, 0);
        var reservation = ReservationModel.Create(
            pasien,
            plannedDate,
            new KelasReff("K1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal Melati"),
            "user1",
            _defaultTime);

        // Act
        var result = _sut.RecordReservationCreated(reservation, _defaultTime);

        // Assert
        result.Person.PersonName.Should().Be("Hartono");
        result.Person.TglLahir.Should().Be(new DateOnly(1975, 12, 1));
        result.VisitDate.Should().Be(new DateOnly(2026, 11, 1));
        result.ListEvent.Should().HaveCount(1);
        result.ListEvent.First().EventName.Should().Be(PasienTrackerInpatientEvents.Reservasi);
        result.ListEvent.First().ReffId.Should().Be(reservation.ReservationId);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordOpnameRequestCreated_WithParameters_CreatesTrackerWithOpnameRequestEventAndSaves()
    {
        // Arrange
        var personName = "Dewi Lestari";
        var tglLahir = new DateOnly(1988, 8, 8);
        var visitDate = new DateOnly(2026, 10, 18);
        var opnameRequestId = "OPN-20261018-001";
        var occurredAt = new DateTime(2026, 10, 17, 11, 0, 0);

        // Act
        var result = _sut.RecordOpnameRequestCreated(personName, tglLahir, visitDate, opnameRequestId, occurredAt);

        // Assert
        result.Should().NotBeNull();
        result.Person.PersonName.Should().Be(personName);
        result.Person.TglLahir.Should().Be(tglLahir);
        result.VisitDate.Should().Be(visitDate);
        result.ListEvent.Should().HaveCount(1);

        var firstEvent = result.ListEvent.First();
        firstEvent.EventName.Should().Be(PasienTrackerInpatientEvents.OpnameRequest);
        firstEvent.ReffId.Should().Be(opnameRequestId);
        firstEvent.EventDate.Should().Be(occurredAt);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordOpnameRequestCreated_WithOpnameRequestModel_CreatesTrackerCorrectly()
    {
        // Arrange
        var pasien = new PasienReff("PAS-002", "Rudi Hartono", new DateOnly(1980, 1, 10), "L");
        var dokter = new PpaReff("D001", "dr. Subroto, Sp.PD");
        var plannedDate = new DateTime(2026, 10, 19, 8, 0, 0);
        var opname = OpnameRequestModel.Create(
            pasien,
            dokter,
            plannedDate,
            "Indikasi rawat inap demam tifoid",
            "user1",
            "TRS-001",
            _defaultTime);

        // Act
        var result = _sut.RecordOpnameRequestCreated(opname, _defaultTime);

        // Assert
        result.Person.PersonName.Should().Be("Rudi Hartono");
        result.Person.TglLahir.Should().Be(new DateOnly(1980, 1, 10));
        result.VisitDate.Should().Be(new DateOnly(2026, 10, 19));
        result.ListEvent.Should().HaveCount(1);
        result.ListEvent.First().EventName.Should().Be(PasienTrackerInpatientEvents.OpnameRequest);
        result.ListEvent.First().ReffId.Should().Be(opname.OpnameRequestId);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_WhenExistingTrackerMatchesSourceRefId_AppendsRegistrasiEvent()
    {
        // Arrange
        var personName = "Budi Rahardjo";
        var tglLahir = new DateOnly(1985, 5, 20);
        var visitDate = new DateOnly(2026, 10, 20);
        var reservationId = "RSV-20261020-001";
        var regId = "REG-99901";
        var registrationTime = new DateTime(2026, 10, 20, 14, 0, 0);

        var existingTracker = PasienTrackerModel.Create(
            new PersonType(personName, tglLahir),
            visitDate,
            PasienTrackerInpatientEvents.Reservasi,
            reservationId,
            new DateTime(2026, 10, 17, 8, 0, 0));

        var headerView = new PasienTrackerView(
            existingTracker.PasienTrackerId,
            existingTracker.Person,
            existingTracker.VisitDate,
            existingTracker.StartPeriod,
            existingTracker.LastPeriod);

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([headerView]);
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == existingTracker.PasienTrackerId)))
            .Returns(MayBe.From(existingTracker));

        // Act
        var result = _sut.RecordAdmissionRegistered(personName, tglLahir, visitDate, regId, reservationId, registrationTime);

        // Assert
        result.Should().BeSameAs(existingTracker);
        result.ListEvent.Should().HaveCount(2);

        var events = result.ListEvent.ToList();
        events[0].EventName.Should().Be(PasienTrackerInpatientEvents.Reservasi);
        events[0].ReffId.Should().Be(reservationId);

        events[1].EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        events[1].ReffId.Should().Be(regId);
        events[1].EventDate.Should().Be(registrationTime);
        events[1].NoUrut.Should().Be(2);

        _trackerRepoMock.Verify(x => x.SaveChanges(existingTracker), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_WhenNoSourceRefIdProvided_MatchesCandidateAndAppendsRegistrasi()
    {
        // Arrange
        var personName = "Dewi Lestari";
        var tglLahir = new DateOnly(1988, 8, 8);
        var visitDate = new DateOnly(2026, 10, 18);
        var regId = "REG-88802";

        var existingTracker = PasienTrackerModel.Create(
            new PersonType(personName, tglLahir),
            visitDate,
            PasienTrackerInpatientEvents.OpnameRequest,
            "OPN-123",
            _defaultTime);

        var headerView = new PasienTrackerView(
            existingTracker.PasienTrackerId,
            existingTracker.Person,
            existingTracker.VisitDate,
            existingTracker.StartPeriod,
            existingTracker.LastPeriod);

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([headerView]);
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == existingTracker.PasienTrackerId)))
            .Returns(MayBe.From(existingTracker));

        // Act
        var result = _sut.RecordAdmissionRegistered(personName, tglLahir, visitDate, regId);

        // Assert
        result.Should().BeSameAs(existingTracker);
        result.ListEvent.Should().HaveCount(2);
        result.ListEvent.Last().EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        result.ListEvent.Last().ReffId.Should().Be(regId);

        _trackerRepoMock.Verify(x => x.SaveChanges(existingTracker), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_GracefulFallback_WhenNoExistingTrackerFound()
    {
        // Arrange
        var personName = "Pasien Baru";
        var tglLahir = new DateOnly(1995, 4, 12);
        var visitDate = new DateOnly(2026, 10, 21);
        var regId = "REG-77703";
        var reservationId = "RSV-NOT-FOUND";

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([]);

        // Act
        var result = _sut.RecordAdmissionRegistered(personName, tglLahir, visitDate, regId, reservationId, _defaultTime);

        // Assert
        result.Should().NotBeNull();
        result.Person.PersonName.Should().Be(personName);
        result.VisitDate.Should().Be(visitDate);
        result.ListEvent.Should().HaveCount(1);

        var fallbackEvent = result.ListEvent.First();
        fallbackEvent.EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        fallbackEvent.ReffId.Should().Be(regId);
        fallbackEvent.EventDate.Should().Be(_defaultTime);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_GracefulFallback_WhenSourceRefIdDoesNotMatchCandidate()
    {
        // Arrange
        var personName = "Agus Salim";
        var tglLahir = new DateOnly(1970, 7, 7);
        var visitDate = new DateOnly(2026, 10, 22);
        var regId = "REG-66604";
        var wantedReservationId = "RSV-WANTED";

        var existingTracker = PasienTrackerModel.Create(
            new PersonType(personName, tglLahir),
            visitDate,
            "SOME-OTHER-EVENT",
            "OTHER-REF",
            _defaultTime);

        var headerView = new PasienTrackerView(
            existingTracker.PasienTrackerId,
            existingTracker.Person,
            existingTracker.VisitDate,
            existingTracker.StartPeriod,
            existingTracker.LastPeriod);

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Returns([headerView]);
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == existingTracker.PasienTrackerId)))
            .Returns(MayBe.From(existingTracker));

        // Act
        var result = _sut.RecordAdmissionRegistered(personName, tglLahir, visitDate, regId, wantedReservationId, _defaultTime);

        // Assert: fallback new tracker created directly with Registrasi
        result.Should().NotBeNull();
        result.Should().NotBeSameAs(existingTracker);
        result.ListEvent.Should().HaveCount(1);
        result.ListEvent.First().EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        result.ListEvent.First().ReffId.Should().Be(regId);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_GracefulFallback_WhenRepositoryThrowsException()
    {
        // Arrange
        var personName = "Anomali Error";
        var tglLahir = new DateOnly(1982, 11, 23);
        var visitDate = new DateOnly(2026, 10, 23);
        var regId = "REG-55505";

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), tglLahir))
            .Throws(new InvalidOperationException("Database connection timeout"));

        // Act
        var result = _sut.RecordAdmissionRegistered(personName, tglLahir, visitDate, regId, "RSV-ERR", _defaultTime);

        // Assert: should not throw, should gracefully create and save fallback tracker
        result.Should().NotBeNull();
        result.Person.PersonName.Should().Be(personName);
        result.ListEvent.Should().HaveCount(1);
        result.ListEvent.First().EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        result.ListEvent.First().ReffId.Should().Be(regId);

        _trackerRepoMock.Verify(x => x.SaveChanges(result), Times.Once);
    }

    [Fact]
    public void RecordAdmissionRegistered_WithReservationModel_AppendsRegistrasiCorrectly()
    {
        // Arrange
        var pasien = new PasienReff("PAS-010", "Sri Wahyuni", new DateOnly(1987, 6, 14), "P");
        var plannedDate = new DateTime(2026, 10, 24, 10, 0, 0);
        var reservation = ReservationModel.Create(
            pasien,
            plannedDate,
            new KelasReff("K2", "Kelas 2"),
            new BangsalReff("B2", "Bangsal Mawar"),
            "user1",
            _defaultTime);

        var regId = "REG-44406";

        var existingTracker = PasienTrackerModel.Create(
            new PersonType(pasien.PasienName, pasien.TglLahir),
            DateOnly.FromDateTime(plannedDate),
            PasienTrackerInpatientEvents.Reservasi,
            reservation.ReservationId,
            _defaultTime);

        var headerView = new PasienTrackerView(
            existingTracker.PasienTrackerId,
            existingTracker.Person,
            existingTracker.VisitDate,
            existingTracker.StartPeriod,
            existingTracker.LastPeriod);

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), pasien.TglLahir))
            .Returns([headerView]);
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == existingTracker.PasienTrackerId)))
            .Returns(MayBe.From(existingTracker));

        // Act
        var result = _sut.RecordAdmissionRegistered(reservation, regId, _defaultTime);

        // Assert
        result.Should().BeSameAs(existingTracker);
        result.ListEvent.Should().HaveCount(2);
        result.ListEvent.Last().EventName.Should().Be(PasienTrackerInpatientEvents.Registrasi);
        result.ListEvent.Last().ReffId.Should().Be(regId);
    }
}
