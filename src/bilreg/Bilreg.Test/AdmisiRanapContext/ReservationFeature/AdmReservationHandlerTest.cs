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
            _auditRepoMock.Object);

        var response = await handler.Handle(
            new AdmCreateReservationCmd(
                "P001",
                new DateTime(2026, 8, 1),
                "K1",
                "B1",
                "user1"),
            CancellationToken.None);

        response.ReservationId.Should().StartWith("RSV");
        saved.Should().NotBeNull();
        saved!.ReservationStatus.Should().Be(ReservationStatusEnum.Reserved);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
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
            _auditRepoMock.Object);

        await handler.Handle(
            new AdmMaintainReservationCmd(
                reserved.ReservationId,
                new DateTime(2026, 8, 5),
                "K2",
                "B2",
                "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.ReservationStatus.Should().Be(ReservationStatusEnum.Maintained);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
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
