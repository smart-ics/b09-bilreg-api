using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.ReservationFeature;

public class AdmReservationHandlerTest
{
    private readonly Mock<IReservationRepo> _reservationRepoMock = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGatewayMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _trackerCoordinatorMock = new();

    [Fact]
    public async Task UT01_GivenValidRequest_WhenCreate_ThenSavesReservation()
    {
        SetupMasters();
        ReservationModel? saved = null;
        _reservationRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
            .Callback<ReservationModel>(m => saved = m);

        var handler = new AdmCreateReservationHandler(
            _reservationRepoMock.Object,
            _patientGatewayMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object, TestTglJamProvider.Instance,
            _trackerCoordinatorMock.Object);

        var response = await handler.Handle(
            new AdmCreateReservationCmd(
                "P001",
                "2026-08-01",
                "K1",
                "B1",
                "user1"),
            CancellationToken.None);

        response.ReservationId.Should().StartWith("RSV");
        saved.Should().NotBeNull();
        saved!.ReservationStatus.Should().Be(ReservationStatusEnum.Reserved);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        _trackerCoordinatorMock.Verify(x => x.RecordReservationCreated(
            It.Is<ReservationModel>(r => r.ReservationId == response.ReservationId),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenReservedReservation_WhenMaintain_ThenSavesMaintained()
    {
        var reserved = ReservationModel.Create(
            SamplePasienReff(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        _reservationRepoMock
            .Setup(x => x.LoadEntity(It.Is<IReservationKey>(k => k.ReservationId == reserved.ReservationId)))
            .Returns(MayBe.From(reserved));
        SetupMasters();

        ReservationModel? saved = null;
        _reservationRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
            .Callback<ReservationModel>(m => saved = m);

        var handler = new AdmMaintainReservationHandler(
            _reservationRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object, TestTglJamProvider.Instance);

        await handler.Handle(
            new AdmMaintainReservationCmd(
                reserved.ReservationId,
                "2026-08-05",
                "K2",
                "B2",
                "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.ReservationStatus.Should().Be(ReservationStatusEnum.Maintained);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT03_GivenReservedReservation_WhenCancel_ThenSavesCancelledAndLogsAuditWithReason()
    {
        var reserved = ReservationModel.Create(
            SamplePasienReff(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        _reservationRepoMock
            .Setup(x => x.LoadEntity(It.Is<IReservationKey>(k => k.ReservationId == reserved.ReservationId)))
            .Returns(MayBe.From(reserved));

        ReservationModel? saved = null;
        _reservationRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
            .Callback<ReservationModel>(m => saved = m);

        AuditLog? loggedAudit = null;
        _auditRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(a => loggedAudit = a);

        var handler = new AdmCancelReservationHandler(
            _reservationRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        const string reason = "Pasien membatalkan rencana rawat inap";
        await handler.Handle(
            new AdmCancelReservationCmd(reserved.ReservationId, "user2", reason),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.ReservationStatus.Should().Be(ReservationStatusEnum.Cancelled);
        saved.AuditTrail.Voided.UserId.Should().Be("user2");

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        loggedAudit.Should().NotBeNull();
        loggedAudit!.ActionType.Should().Be("VOID");
        loggedAudit.EntityName.Should().Be(nameof(ReservationModel));
        loggedAudit.EntityId.Should().Be(reserved.ReservationId);
        loggedAudit.UserId.Should().Be("user2");
        loggedAudit.Reason.Should().Be(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UT04_GivenInvalidReason_WhenCancel_ThenThrowsArgumentException(string? invalidReason)
    {
        var handler = new AdmCancelReservationHandler(
            _reservationRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new AdmCancelReservationCmd("RSV001", "user1", invalidReason!),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private void SetupMasters()
    {
        _patientGatewayMock
            .Setup(x => x.ResolvePatient("P001"))
            .Returns(SamplePasienReff());

        _wardGatewayMock
            .Setup(x => x.ResolveKelas("K1"))
            .Returns(new KelasReff("K1", "Kelas 1"));
        _wardGatewayMock
            .Setup(x => x.ResolveKelas("K2"))
            .Returns(new KelasReff("K2", "Kelas 2"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsal("B1"))
            .Returns(new BangsalReff("B1", "Bangsal A"));
        _wardGatewayMock
            .Setup(x => x.ResolveBangsal("B2"))
            .Returns(new BangsalReff("B2", "Bangsal B"));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
