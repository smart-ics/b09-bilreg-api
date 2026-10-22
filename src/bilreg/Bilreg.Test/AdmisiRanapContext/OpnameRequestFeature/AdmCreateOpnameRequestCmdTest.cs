using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class AdmCreateOpnameRequestCmdTest
{
    private readonly Mock<IOpnameRequestRepo> _opnameRequestRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IDoctorServiceGateway> _doctorGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _trackerCoordinatorMock = new();

    public AdmCreateOpnameRequestCmdTest()
    {
        _patientGatewayMock
            .Setup(x => x.ResolvePatient("P001"))
            .Returns(new PasienReff("P001", "Budi Santoso", new DateOnly(1990, 5, 20), "L"));

        _doctorGatewayMock
            .Setup(x => x.ResolveDoctor("D001"))
            .Returns(new PpaReff("D001", "Dr. Setiawan"));
    }

    [Fact]
    public async Task Handle_GivenValidRequest_InvokesCoordinatorWithCreatedOpnameRequest()
    {
        // Arrange
        OpnameRequestModel? savedOpnameRequest = null;
        _opnameRequestRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(r => savedOpnameRequest = r);

        var handler = new AdmCreateOpnameRequestHandler(
            _opnameRequestRepoMock.Object,
            _patientGatewayMock.Object,
            _doctorGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var cmd = new AdmCreateOpnameRequestCmd(
            "P001",
            "D001",
            "2026-09-01",
            "Perlu rawat inap",
            "ORD-001",
            "user1");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.OpnameRequestId.Should().StartWith("OPN");
        response.TrsOrderId.Should().Be("ORD-001");
        savedOpnameRequest.Should().NotBeNull();
        savedOpnameRequest!.OpnameRequestId.Should().Be(response.OpnameRequestId);

        _trackerCoordinatorMock.Verify(x => x.RecordOpnameRequestCreated(
            It.Is<OpnameRequestModel>(r => r.OpnameRequestId == response.OpnameRequestId && r.Pasien.PasienName == "Budi Santoso"),
            It.IsAny<DateTime>()), Times.Once);

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenValidRequest_WithRealCoordinator_CreatesPasienTrackerWithEventOpnameRequestAndOpnameRequestId()
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

        var handler = new AdmCreateOpnameRequestHandler(
            _opnameRequestRepoMock.Object,
            _patientGatewayMock.Object,
            _doctorGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            realCoordinator);

        var cmd = new AdmCreateOpnameRequestCmd(
            "P001",
            "D001",
            "2026-09-01",
            "Perlu rawat inap",
            "ORD-001",
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
        initialEvent.EventName.Should().Be(PasienTrackerInpatientEvents.OpnameRequest);
        initialEvent.EventName.Should().Be("OpnameRequest");
        initialEvent.ReffId.Should().Be(response.OpnameRequestId);
        initialEvent.NoUrut.Should().Be(1);
    }

    [Theory]
    [InlineData("", "D001", "2026-09-01", "ORD-001", "user1")]
    [InlineData("P001", "", "2026-09-01", "ORD-001", "user1")]
    [InlineData("P001", "D001", "", "ORD-001", "user1")]
    [InlineData("P001", "D001", "2026-09-01", "", "user1")]
    [InlineData("P001", "D001", "2026-09-01", "ORD-001", "")]
    public async Task Handle_GivenInvalidParameters_ThrowsArgumentException(
        string pasienId, string dokterId, string plannedDate, string trsOrderId, string userId)
    {
        // Arrange
        var handler = new AdmCreateOpnameRequestHandler(
            _opnameRequestRepoMock.Object,
            _patientGatewayMock.Object,
            _doctorGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var cmd = new AdmCreateOpnameRequestCmd(pasienId, dokterId, plannedDate, "Catatan", trsOrderId, userId);

        // Act & Assert
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
