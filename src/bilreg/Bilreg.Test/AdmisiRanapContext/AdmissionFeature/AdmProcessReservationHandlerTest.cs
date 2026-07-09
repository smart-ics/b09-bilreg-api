using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmProcessReservationHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IReservationRepo> _reservationRepoMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenReservedReservation_WhenProcess_ThenRealizesAndSavesBoth()
    {
        var reservation = ReservationModel.Create(
            SamplePasienReff(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        SetupMasters();
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([]);
        _reservationRepoMock
            .Setup(x => x.LoadEntity(It.Is<IReservationKey>(k => k.ReservationId == reservation.ReservationId)))
            .Returns(MayBe.From(reservation));

        ReservationModel? savedReservation = null;
        AdmissionModel? savedAdmission = null;
        _reservationRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<ReservationModel>()))
            .Callback<ReservationModel>(m => savedReservation = m);
        _admissionRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(m => savedAdmission = m);

        var handler = CreateHandler();
        var response = await handler.Handle(
            new AdmProcessReservationCmd(reservation.ReservationId, "1", "B1", "user2"),
            CancellationToken.None);

        response.RegId.Should().StartWith("RG");
        savedAdmission.Should().NotBeNull();
        savedReservation.Should().NotBeNull();
        savedReservation!.ReservationStatus.Should().Be(ReservationStatusEnum.Realized);
        savedReservation.RealizedRegId.Should().Be(response.RegId);
        _admissionRepoMock.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Once);
        _reservationRepoMock.Verify(x => x.SaveChanges(It.IsAny<ReservationModel>()), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UT02_GivenRealizedReservation_WhenProcess_ThenThrows()
    {
        var reservation = ReservationModel.Create(
                SamplePasienReff(),
                new DateTime(2026, 8, 1),
                SampleKelas(),
                SampleBangsal(),
                "user1")
            .Maintain(new DateTime(2026, 8, 1), SampleKelas(), SampleBangsal(), "user1")
            .Realize("RG00000099", "user1");

        SetupMasters();
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([]);
        _reservationRepoMock
            .Setup(x => x.LoadEntity(It.Is<IReservationKey>(k => k.ReservationId == reservation.ReservationId)))
            .Returns(MayBe.From(reservation));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new AdmProcessReservationCmd(reservation.ReservationId, "1", "B1", "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Maintained*");
    }

    private AdmProcessReservationHandler CreateHandler() =>
        new(
            _admissionRepoMock.Object,
            _reservationRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object);

    private void SetupMasters()
    {
        _wardGatewayMock
            .Setup(x => x.ResolveKelasDk("1"))
            .Returns(new KelasDkType("1", "Kelas DK 1"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal A"));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
