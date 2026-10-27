using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.ReservationFeature;

public class AdmCreateReservationCmdTest
{
    private readonly Mock<IReservationRepo> _reservationRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _trackerCoordinatorMock = new();

    public AdmCreateReservationCmdTest()
    {
        _patientGatewayMock
            .Setup(x => x.ResolvePatient("P001"))
            .Returns(new PasienReff("P001", "Budi Santoso", new DateOnly(1990, 5, 20), "L"));

        _wardGatewayMock
            .Setup(x => x.ResolveKelas("K1"))
            .Returns(new KelasReff("K1", "Kelas 1"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsal("B1"))
            .Returns(new BangsalReff("B1", "Bangsal Mawar"));
    }

    [Fact]
    public async Task Handle_GivenValidRequest_InvokesCoordinatorWithCreatedReservation()
    {
        // Arrange
        ReservationModel? savedReservation = null;
        _reservationRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
            .Callback<ReservationModel>(r => savedReservation = r);

        var handler = new AdmCreateReservationHandler(
            _reservationRepoMock.Object,
            _patientGatewayMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var cmd = new AdmCreateReservationCmd(
            "P001",
            "2026-09-01",
            "K1",
            "B1",
            "user1");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.ReservationId.Should().StartWith("RSV");
        savedReservation.Should().NotBeNull();
        savedReservation!.ReservationId.Should().Be(response.ReservationId);

        _trackerCoordinatorMock.Verify(x => x.RecordReservationCreated(
            It.Is<ReservationModel>(r => r.ReservationId == response.ReservationId && r.Pasien.PasienName == "Budi Santoso"),
            It.IsAny<DateTime>()), Times.Once);

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenValidRequest_WithRealCoordinator_CreatesPasienTrackerWithEventReservasiAndReservationId()
    {
        // Arrange: Wire real coordinator with mock tracker repo to verify end-to-end event recording contract
        var trackerRepoMock = new Mock<IPasienTrackerRepo>();
        PasienTrackerModel? savedTracker = null;
        trackerRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var realCoordinator = new PasienTrackerInpatientCoordinator(
            trackerRepoMock.Object,
            TestTglJamProvider.Instance);

        var handler = new AdmCreateReservationHandler(
            _reservationRepoMock.Object,
            _patientGatewayMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            realCoordinator);

        var cmd = new AdmCreateReservationCmd(
            "P001",
            "2026-09-01",
            "K1",
            "B1",
            "user1");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        savedTracker.Should().NotBeNull();
        savedTracker!.Person.PersonName.Should().Be("Budi Santoso");
        savedTracker.Person.TglLahir.Should().Be(new DateOnly(1990, 5, 20));
        savedTracker.VisitDate.Should().Be(new DateOnly(2026, 9, 1));
        savedTracker.ListEvent.Should().HaveCount(1);

        var initialEvent = savedTracker.ListEvent.First();
        initialEvent.EventName.Should().Be(PasienTrackerInpatientEvents.Reservasi);
        initialEvent.EventName.Should().Be("Reservasi");
        initialEvent.ReffId.Should().Be(response.ReservationId);
        initialEvent.NoUrut.Should().Be(1);
    }

    [Theory]
    [InlineData("", "2026-09-01", "K1", "B1", "user1")]
    [InlineData("P001", "", "K1", "B1", "user1")]
    [InlineData("P001", "2026-09-01", "", "B1", "user1")]
    [InlineData("P001", "2026-09-01", "K1", "", "user1")]
    [InlineData("P001", "2026-09-01", "K1", "B1", "")]
    public async Task Handle_GivenInvalidParameters_ThrowsArgumentException(
        string pasienId, string plannedDate, string kelasId, string bangsalId, string userId)
    {
        // Arrange
        var handler = new AdmCreateReservationHandler(
            _reservationRepoMock.Object,
            _patientGatewayMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var cmd = new AdmCreateReservationCmd(pasienId, plannedDate, kelasId, bangsalId, userId);

        // Act & Assert
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
