using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueKioskUseCasesTest
{
    private readonly Mock<IAdmissionQueueKioskRepo> _kioskRepoMock = new();
    private readonly Mock<IAdmissionServicePointRepo> _servicePointRepoMock = new();
    private readonly Mock<ITglJamProvider> _clockMock = new();

    private readonly DateTime _now = new(2026, 11, 18, 10, 0, 0);

    public AdmissionQueueKioskUseCasesTest()
    {
        _clockMock.Setup(c => c.Now).Returns(_now);
    }

    [Fact]
    public async Task CreateKioskHandler_PharmacyOnlyActiveKiosk_SavesAndReturnsDepoFarmasiId()
    {
        _kioskRepoMock
            .Setup(r => r.LoadEntity(It.Is<IAdmissionQueueKioskKey>(k => k.StationId == "KIOSK-FARM-01")))
            .Returns(MayBe<AdmissionQueueKioskModel>.None);

        AdmissionQueueKioskModel? savedModel = null;
        _kioskRepoMock
            .Setup(r => r.SaveChanges(It.IsAny<AdmissionQueueKioskModel>()))
            .Callback<AdmissionQueueKioskModel>(m => savedModel = m);

        var sut = new CreateKioskHandler(_kioskRepoMock.Object, _servicePointRepoMock.Object, _clockMock.Object);

        var cmd = new CreateKioskCmd(
            "KIOSK-FARM-01",
            "Kiosk Farmasi 1",
            "Lantai 1 Farmasi",
            true,
            5050,
            "Khusus farmasi",
            Array.Empty<AdmissionKioskServicePointDtoResponse>(),
            "admin-1",
            "DEPO-FAR-01");

        var response = await sut.Handle(cmd, CancellationToken.None);

        savedModel.Should().NotBeNull();
        savedModel!.DepoFarmasiId.Should().Be("DEPO-FAR-01");
        savedModel.ServicePoints.Should().BeEmpty();
        response.StationId.Should().Be("KIOSK-FARM-01");
        response.DepoFarmasiId.Should().Be("DEPO-FAR-01");
    }

    [Fact]
    public async Task UpdateKioskHandler_UpdatesAndReturnsDepoFarmasiId()
    {
        var existing = AdmissionQueueKioskModel.Load(
            "KIOSK-01",
            "Kiosk Lobby",
            "Lobby",
            true,
            5050,
            string.Empty,
            new[] { new AdmissionKioskServicePointMapping("SP-ADM-01", 1) },
            2,
            "admin-1",
            _now.AddDays(-1),
            "admin-1",
            _now.AddDays(-1),
            string.Empty);

        _kioskRepoMock
            .Setup(r => r.LoadEntity(It.Is<IAdmissionQueueKioskKey>(k => k.StationId == "KIOSK-01")))
            .Returns(MayBe.From(existing));

        AdmissionQueueKioskModel? savedModel = null;
        _kioskRepoMock
            .Setup(r => r.SaveChanges(It.IsAny<AdmissionQueueKioskModel>()))
            .Callback<AdmissionQueueKioskModel>(m => savedModel = m);

        var sut = new UpdateKioskHandler(_kioskRepoMock.Object, _clockMock.Object);

        var cmd = new UpdateKioskCmd(
            "KIOSK-01",
            "Kiosk Hybrid",
            "Lobby Utama",
            5051,
            "Updated notes",
            "2",
            "admin-2",
            "DEPO-FAR-02");

        var response = await sut.Handle(cmd, CancellationToken.None);

        savedModel.Should().NotBeNull();
        savedModel!.DepoFarmasiId.Should().Be("DEPO-FAR-02");
        savedModel.RowVersion.Should().Be(3);
        response.DepoFarmasiId.Should().Be("DEPO-FAR-02");
        response.DisplayName.Should().Be("Kiosk Hybrid");
    }

    [Fact]
    public async Task GetKioskBootConfigHandler_PharmacyOnlyActiveKiosk_ReturnsBootConfigWithDepoFarmasiId()
    {
        var kiosk = AdmissionQueueKioskModel.Create(
            "KIOSK-FARM-01",
            "Kiosk Farmasi 1",
            "Area Farmasi",
            true,
            5050,
            string.Empty,
            Array.Empty<AdmissionKioskServicePointMapping>(),
            "admin-1",
            _now,
            "DEPO-FAR-01");

        _kioskRepoMock
            .Setup(r => r.LoadEntity(It.Is<IAdmissionQueueKioskKey>(k => k.StationId == "KIOSK-FARM-01")))
            .Returns(MayBe.From(kiosk));

        var sut = new GetKioskBootConfigHandler(_kioskRepoMock.Object);

        var response = await sut.Handle(new GetKioskBootConfigQry("KIOSK-FARM-01"), CancellationToken.None);

        response.DeviceId.Should().Be("KIOSK-FARM-01");
        response.Role.Should().Be("kiosk");
        response.ServicePointIds.Should().BeEmpty();
        response.DepoFarmasiId.Should().Be("DEPO-FAR-01");
    }

    [Fact]
    public async Task GetKioskBootConfigHandler_ActiveKioskWithoutServicePointAndDepo_ThrowsKioskMappingRequired()
    {
        var kiosk = AdmissionQueueKioskModel.Load(
            "KIOSK-EMPTY",
            "Kiosk Kosong",
            "Lobby",
            true,
            5050,
            string.Empty,
            Array.Empty<AdmissionKioskServicePointMapping>(),
            1,
            "admin-1",
            _now,
            "admin-1",
            _now,
            string.Empty);

        _kioskRepoMock
            .Setup(r => r.LoadEntity(It.Is<IAdmissionQueueKioskKey>(k => k.StationId == "KIOSK-EMPTY")))
            .Returns(MayBe.From(kiosk));

        var sut = new GetKioskBootConfigHandler(_kioskRepoMock.Object);

        var act = () => sut.Handle(new GetKioskBootConfigQry("KIOSK-EMPTY"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AdmissionQueueConfigurationException>();
        ex.Which.Code.Should().Be(AdmissionQueueConfigurationErrorCodes.KioskMappingRequired);
    }
}
