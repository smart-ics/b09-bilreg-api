using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.ReservationFeature;

public class AdmReservationHandlerTest
{
    private readonly Mock<IReservationRepo> _reservationRepoMock = new();
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();

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
            _pasienRepoMock.Object,
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object);

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
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object);

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
    }

    private void SetupMasters()
    {
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe.From(PasienModel.Key("P001") as PasienModel));

        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K1")))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1", KelasName = "Kelas 1" }));
        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K2")))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K2", KelasName = "Kelas 2" }));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B1")))
            .Returns(MayBe.From(CreateBangsal("B1", "Bangsal A")));
        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B2")))
            .Returns(MayBe.From(CreateBangsal("B2", "Bangsal B")));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    private static BangsalType CreateBangsal(string id, string name) =>
        new(id, name, RoomCatType.Default, new Bilreg.Domain.AdmisiContext.LayananFeature.LayananReff("-", "-"));
}
